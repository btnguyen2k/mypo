using Ddth.Utilities.Tempus;
using Finhub.Client;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Portfolio.Shared.Utils;

public static class ReportUtils
{
    private static (string itemCode, MarketDef? market) NormalizeItemCode(IEnumerable<MarketDef> markets, string? refMarketId, string? refItemCode)
    {
        var market = markets.FirstOrDefault(m => m.Id == refMarketId);
        if (string.IsNullOrEmpty(refItemCode)) return (ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, market);
        if (refItemCode.Contains(':', StringComparison.Ordinal)) return (refItemCode, market); // already normalized

        var marketCode = market?.Code?.ToUpper();
        return ($"{(marketCode is null ? string.Empty : marketCode+":")}{refItemCode}", market);
    }

    /// <summary>
    /// Builds the item report entry for the given PnL summary (with report type, and report period). The entry represents the snapshot position of the symbol for the report period.
    /// </summary>
    /// <param name="pnlSummary"></param>
    /// <param name="markets"></param>
    /// <param name="reportType"></param>
    /// <param name="dateInReportPeriod"></param>
    /// <param name="portfolioRepository"></param>
    /// <param name="finHubClient"></param>
    /// <param name="quoteHistoryCache"></param>
    /// <param name="weekStartDay"></param>
    /// <param name="fiscalYearStartMonth"></param>
    /// <returns></returns>
    public static async ValueTask<ReportEntity?> BuildItemReport(
        PnlSummary pnlSummary,
        IEnumerable<MarketDef> markets,
        ReportType reportType,
        DateTimeOffset dateInReportPeriod,
        IPortfolioRepository portfolioRepository,
        IFinHubClient finHubClient,
        IDictionary<string, HistoryPoint[]> quoteHistoryCache,
        DayOfWeek weekStartDay = DayOfWeek.Monday,
        int fiscalYearStartMonth = 1)
    {
        // quick validation
        var (itemCode, market) = NormalizeItemCode(markets, pnlSummary.RefMarketId, pnlSummary.RefItemCode);
        if (itemCode == ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO) return null;

        // initial setup
        var (label, startDate, endDate) = ComputePeriod(reportType, dateInReportPeriod, weekStartDay, fiscalYearStartMonth);
        var periodStart = startDate.ToString("yyyy-MM-dd");
        var nowLocal = (DateTimeOffset.Now.ToTimeZoneSilently(market?.TimeZone ?? "UTC") ?? DateTimeOffset.UtcNow).StartOfDay();
        var isFinal = endDate < nowLocal;

        // first, create a new ReportEntity instance
        var entry = new ReportEntity
        {
                Type = reportType,
                PeriodStart = periodStart,
                PeriodLabel = label,
                PortfolioId = pnlSummary.PortfolioId,
                ItemCode = itemCode,
                TxType = ReportEntity.TX_TYPE_POSITION,
                Metadata = new ReportEntityMetadata
                {
                    Quantity = pnlSummary.TotalBuyQuantity - pnlSummary.TotalSellQuantity,
                    // Cost = pnlSummary.TotalBuyValue - pnlSummary.TotalSellValue,
                    Buys = pnlSummary.TotalBuyValue,
                    Sells = pnlSummary.TotalSellValue,
                    Tax = pnlSummary.TotalTax,
                    Fees = pnlSummary.TotalFees,
                    Interest = pnlSummary.TotalInterest,
                    Cashin = pnlSummary.TotalCashIn,
                    Cashout = pnlSummary.TotalCashOut,
                    Dividends = pnlSummary.TotalDividends,
                    Distributions = pnlSummary.TotalDistributions,
                },
                IsFinal = isFinal,
        };

        // next, fetch the immediately-preceding period's position entry to carry the cumulative holdings forward
        var prevEntry = (await portfolioRepository.GetPrevReportAsync(entry)) ?? new();
        prevEntry.Metadata ??= new();

        // then, carry the running holding forward
        entry.Metadata.AccumulatedQuantity = (entry.Metadata.Quantity??0m) + (prevEntry.Metadata.AccumulatedQuantity??0m);
        entry.Metadata.AccumulatedBuys = (entry.Metadata.Buys??0m) + (prevEntry.Metadata.AccumulatedBuys??0m);
        entry.Metadata.AccumulatedSells = (entry.Metadata.Sells??0m) + (prevEntry.Metadata.AccumulatedSells??0m);
        // entry.Metadata.AccumulatedCost = (entry.Metadata.Cost??0m) + (prevEntry.Metadata.AccumulatedCost??0m);
        entry.Metadata.AccumulatedTax = (entry.Metadata.Tax??0m) + (prevEntry.Metadata.AccumulatedTax??0m);
        entry.Metadata.AccumulatedFees = (entry.Metadata.Fees??0m) + (prevEntry.Metadata.AccumulatedFees??0m);
        entry.Metadata.AccumulatedInterest = (entry.Metadata.Interest??0m) + (prevEntry.Metadata.AccumulatedInterest??0m);
        entry.Metadata.AccumulatedCashin = (entry.Metadata.Cashin??0m) + (prevEntry.Metadata.AccumulatedCashin??0m);
        entry.Metadata.AccumulatedCashout = (entry.Metadata.Cashout??0m) + (prevEntry.Metadata.AccumulatedCashout??0m);
        entry.Metadata.AccumulatedDividends = (entry.Metadata.Dividends??0m) + (prevEntry.Metadata.AccumulatedDividends??0m);
        entry.Metadata.AccumulatedDistributions = (entry.Metadata.Distributions??0m) + (prevEntry.Metadata.AccumulatedDistributions??0m);

        // next, obtain the symbol's quote history, reusing the cache when possible to caculate Open/Close value
        if (!quoteHistoryCache.TryGetValue(entry.ItemCode, out var history))
        {
            var resp = await finHubClient.GetStockQuoteHistoryAsync(entry.ItemCode, 3650);
            history = resp.IsSuccess && resp.Data is not null ? [.. resp.Data] : [];
            quoteHistoryCache[entry.ItemCode] = history;
        }
        // the opening value equals the previous period's closing value so consecutive periods chain seamlessly, e.g. OpenValue(N) = CloseValue(N-1)
        entry.Metadata.OpenValue = prevEntry.Metadata.CloseValue;
        // the closing value marks the cumulative position at the period end using the close price on/before that date.
        // for an in-progress (not-final) period the period end is in the future and has no price yet, so value the
        // position at the latest available date (now) instead, keeping the open position marked-to-market.
        var valuationDate = isFinal ? endDate : nowLocal;
        var closePrice = FindClosePriceForDate(history, valuationDate, itemCode);
        if (itemCode.StartsWith("HNX:")||itemCode.StartsWith("HOSE:")||itemCode.StartsWith("UCOM:"))
        {
            // special case for VN market
            closePrice /= 1000m;
        }
        entry.Metadata.CloseValue = closePrice > 0
            ? (entry.Metadata.AccumulatedQuantity ?? 0m) * closePrice
            : entry.Metadata.OpenValue; // if closing value is not available, use the opening value as a fallback

        // finally return the entry; note: further updates (e.g. cumulative holdings, open/close values) are done in later steps
        // carry the running holding quantity forward: cumulative = previous cumulative + this period's net change
        return entry;
    }

    /// <summary>
    /// Builds the portfolio report entry for the given PnL summary (with report type, and report period). The entry represents the snapshot of the entire portfolio for the given report period.
    /// </summary>
    /// <param name="pnlSummary"></param>
    /// <param name="markets"></param>
    /// <param name="reportType"></param>
    /// <param name="reportPeriod"></param>
    /// <param name="portfolioRepository"></param>
    /// <param name="itemReports"></param>
    /// <param name="weekStartDay"></param>
    /// <param name="fiscalYearStartMonth"></param>
    /// <returns></returns>
    public static async ValueTask<ReportEntity?> BuildPortfolioReport(
        PnlSummary pnlSummary,
        IEnumerable<MarketDef> markets,
        ReportType reportType,
        DateTimeOffset reportPeriod,
        IPortfolioRepository portfolioRepository,
        IEnumerable<ReportEntity> itemReports,
        DayOfWeek weekStartDay = DayOfWeek.Monday,
        int fiscalYearStartMonth = 1)
    {
        // quick validation
        var (itemCode, market) = NormalizeItemCode(markets, pnlSummary.RefMarketId, pnlSummary.RefItemCode);
        if (itemCode != ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO) return null;

        // initial setup
        var (label, startDate, endDate) = ComputePeriod(reportType, reportPeriod, weekStartDay, fiscalYearStartMonth);
        var periodStart = startDate.ToString("yyyy-MM-dd");
        var nowLocal = (DateTimeOffset.Now.ToTimeZoneSilently(market?.TimeZone ?? "UTC") ?? DateTimeOffset.UtcNow).StartOfDay();
        var isFinal = endDate < nowLocal;

        // first, create a new ReportEntity instance
        var entry = new ReportEntity
        {
                Type = reportType,
                PeriodStart = periodStart,
                PeriodLabel = label,
                PortfolioId = pnlSummary.PortfolioId,
                ItemCode = itemCode,
                TxType = ReportEntity.TX_TYPE_POSITION,
                Metadata = new ReportEntityMetadata
                {
                    // Cost = pnlSummary.TotalBuyValue - pnlSummary.TotalSellValue,
                    Buys = pnlSummary.TotalBuyValue,
                    Sells = pnlSummary.TotalSellValue,
                    Tax = pnlSummary.TotalTax,
                    Fees = pnlSummary.TotalFees,
                    Interest = pnlSummary.TotalInterest,
                    Cashin = pnlSummary.TotalCashIn,
                    Cashout = pnlSummary.TotalCashOut,
                    Dividends = pnlSummary.TotalDividends,
                    Distributions = pnlSummary.TotalDistributions,
                },
                IsFinal = isFinal,
        };

        // next, fetch the immediately-preceding period's position entry to carry the cumulative holdings forward
        var prevEntry = (await portfolioRepository.GetPrevReportAsync(entry)) ?? new(){Metadata=new()};
        prevEntry.Metadata ??= new();

        // then, carry the running holding forward
        entry.Metadata.AccumulatedBuys = (entry.Metadata.Buys??0m) + (prevEntry.Metadata.AccumulatedBuys??0m);
        entry.Metadata.AccumulatedSells = (entry.Metadata.Sells??0m) + (prevEntry.Metadata.AccumulatedSells??0m);
        // entry.Metadata.AccumulatedCost = (entry.Metadata.Cost??0m) + (prevEntry.Metadata.AccumulatedCost??0m);
        entry.Metadata.AccumulatedTax = (entry.Metadata.Tax??0m) + (prevEntry.Metadata.AccumulatedTax??0m);
        entry.Metadata.AccumulatedFees = (entry.Metadata.Fees??0m) + (prevEntry.Metadata.AccumulatedFees??0m);
        entry.Metadata.AccumulatedInterest = (entry.Metadata.Interest??0m) + (prevEntry.Metadata.AccumulatedInterest??0m);
        entry.Metadata.AccumulatedCashin = (entry.Metadata.Cashin??0m) + (prevEntry.Metadata.AccumulatedCashin??0m);
        entry.Metadata.AccumulatedCashout = (entry.Metadata.Cashout??0m) + (prevEntry.Metadata.AccumulatedCashout??0m);
        entry.Metadata.AccumulatedDividends = (entry.Metadata.Dividends??0m) + (prevEntry.Metadata.AccumulatedDividends??0m);
        entry.Metadata.AccumulatedDistributions = (entry.Metadata.Distributions??0m) + (prevEntry.Metadata.AccumulatedDistributions??0m);

        // then, compute the portfolio's closing & openning value
        // the opening value equals the previous period's closing value so consecutive periods chain seamlessly, e.g. OpenValue(N) = CloseValue(N-1)
        entry.Metadata.OpenValue = prevEntry.Metadata.CloseValue;
        // the closing value is sum of all item reports' closing values
        var closeValue = itemReports.Where(ir => ir.ItemCode != ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO).Sum(r => r.Metadata?.CloseValue ?? 0m);
        entry.Metadata.CloseValue = closeValue;

        // finally return the entry; note: further updates (e.g. cumulative holdings, open/close values) are done in later steps
        // carry the running holding quantity forward: cumulative = previous cumulative + this period's net change
        return entry;
    }

    /// <summary>
    /// Finds the market close price for <paramref name="targetDate"/> from <paramref name="history"/>, using the close of
    /// the most recent trading day on or before that date (so a non-trading target date such as a weekend or holiday
    /// walks back to the previous trading day). Returns 0 when no price is available, i.e. when the date is earlier
    /// than the first data point (symbol not yet listed) or later than the last data point (date in the future).
    /// </summary>
    private static decimal FindClosePriceForDate(HistoryPoint[] history, DateTimeOffset targetDate, string itemCode="")
    {
        if (history.Length == 0)
        {
            return 0;
        }
        targetDate = targetDate.StartOfDay();
        HistoryPoint? best = null;
        var maxDate = DateTimeOffset.MinValue;
        foreach (var point in history)
        {
            var pointDate = point.Date.StartOfDay();
            if (pointDate > maxDate)
            {
                maxDate = pointDate;
            }
            // keep the latest data point on or before the target date (walking back over non-trading days)
            if (pointDate <= targetDate && (best is null || pointDate > best.Date.StartOfDay()))
            {
                best = point;
            }
        }
        // no price yet for a date beyond the available history (e.g. a period end in the future)
        if (targetDate > maxDate)
        {
            return 0;
        }
        // best is null only when the target date precedes the first data point (symbol not yet listed)
        return best?.Close ?? 0;
    }

    /// <summary>
    /// Converts a report period to a short string representation based on the report type, week start day, and fiscal year start month.
    /// </summary>
    /// <param name="reportType">The type of the report period (weekly/monthly/quarterly/yearly).</param>
    /// <param name="reportPeriod">Any point in time within the target period; it is normalized to the period's start.</param>
    /// <param name="weekStart">The first day of the week (used for weekly periods).</param>
    /// <param name="fiscalYearStartMonth">The first month (1-12) of the fiscal year (used for weekly/quarterly/yearly periods).</param>
    /// <returns>
    /// A short label describing the period. Formats:
    /// <list type="bullet">
    /// <item>Weekly: <c>FY2025-26-W01</c> (week number is relative to the fiscal year)</item>
    /// <item>Monthly: <c>2026-01</c></item>
    /// <item>Quarterly: <c>FY2025-26-Q1</c></item>
    /// <item>Yearly: <c>FY2025-26</c></item>
    /// </list>
    /// where the fiscal year is labeled <c>FY&lt;start-year-4-digits&gt;-&lt;end-year-2-digits&gt;</c>.
    /// </returns>
    public static string ReportPeriodShortStr(ReportType reportType, DateTimeOffset reportPeriod, DayOfWeek weekStart = DayOfWeek.Monday, int fiscalYearStartMonth = 1)
    {
        var (label, _, _) = ComputePeriod(reportType, reportPeriod.Date, weekStart, fiscalYearStartMonth);
        return label;
    }

    /// <summary>
    /// Converts a report period to a long string representation: the short label followed by the period's date range,
    /// e.g. <c>FY2025-26: 2025-07-01 to 2026-06-30</c>.
    /// </summary>
    /// <param name="reportType">The type of the report period (weekly/monthly/quarterly/yearly).</param>
    /// <param name="reportPeriod">Any point in time within the target period; it is normalized to the period's start.</param>
    /// <param name="weekStart">The first day of the week (used for weekly periods).</param>
    /// <param name="fiscalYearStartMonth">The first month (1-12) of the fiscal year (used for weekly/quarterly/yearly periods).</param>
    /// <returns>The short label plus the inclusive period range, e.g. <c>FY2025-26-Q1: 2025-07-01 to 2025-09-30</c>.</returns>
    public static string ReportPeriodLongStr(ReportType reportType, DateTimeOffset reportPeriod, DayOfWeek weekStart = DayOfWeek.Monday, int fiscalYearStartMonth = 1)
    {
        var (label, start, end) = ComputePeriod(reportType, reportPeriod.Date, weekStart, fiscalYearStartMonth);
        return $"{label}: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}";
    }

    /// <summary>
    /// Generates the ordered (oldest-first) list of report periods of <paramref name="reportType"/> that cover the
    /// inclusive date range [<paramref name="fromDate"/> .. <paramref name="toDate"/>].
    /// </summary>
    /// <param name="reportType">The type of the report period (weekly/monthly/quarterly/yearly).</param>
    /// <param name="fromDate">The start date; the first generated period is the one containing this date.</param>
    /// <param name="toDate">The end date; the last generated period is the one containing this date.</param>
    /// <param name="weekStart">The first day of the week (used for weekly periods).</param>
    /// <param name="fiscalYearStartMonth">The first month (1-12) of the fiscal year (used for weekly/quarterly/yearly periods).</param>
    /// <returns>Each period's short label and inclusive start/end dates, oldest first.</returns>
    public static IEnumerable<(string Label, DateTimeOffset Start, DateTimeOffset End)> GenerateReportPeriods(ReportType reportType, DateTimeOffset fromDate, DateTimeOffset toDate, DayOfWeek weekStart = DayOfWeek.Monday, int fiscalYearStartMonth = 1)
    {
        var cursor = fromDate;
        var endDate = toDate;
        while (cursor <= endDate)
        {
            var period = ComputePeriod(reportType, cursor, weekStart, fiscalYearStartMonth);
            yield return period;
            var next = period.End.AddDays(1);
            if (next <= cursor)
            {
                // defensive: the default (unknown type) branch does not advance; stop to avoid an infinite loop
                yield break;
            }
            cursor = next;
        }
    }

    /// <summary>
    /// Computes the short label and the inclusive start/end dates of the report period containing <paramref name="dateInPeriod"/>.
    /// </summary>
    public static (string Label, DateTimeOffset Start, DateTimeOffset End) ComputePeriod(ReportType reportType, DateTimeOffset dateInPeriod, DayOfWeek weekStart, int fiscalYearStartMonth)
    {
        switch (reportType)
        {
            case ReportType.WEEKLY:
            {
                var weekStartDate = dateInPeriod.StartOfWeek(weekStart);
                var fyStart = dateInPeriod.StartOfFiscalYear(fiscalYearStartMonth);
                var week1Start = fyStart.StartOfWeek(weekStart);
                // A week that straddles the fiscal-year boundary belongs to the fiscal year whose week 1 (the week
                // containing that fiscal year's start date) begins on or before this week's start. Without this, the
                // boundary week would be labelled as the trailing week of the outgoing fiscal year (e.g. ...-W53)
                // while the following week jumps to ...-W02 of the new fiscal year, skipping ...-W01.
                var nextFyStart = fyStart.AddYears(1);
                var nextWeek1Start = nextFyStart.StartOfWeek(weekStart);
                if (weekStartDate >= nextWeek1Start)
                {
                    fyStart = nextFyStart;
                    week1Start = nextWeek1Start;
                }
                var weekNumber = (int)((weekStartDate - week1Start).TotalDays / 7) + 1;
                return ($"{FiscalYearLabel(fyStart)}-W{weekNumber:D2}", weekStartDate, weekStartDate.AddDays(6));
            }
            case ReportType.MONTHLY:
            {
                var monthStart = dateInPeriod.StartOfMonth();
                return ($"{monthStart:yyyy-MM}", monthStart, monthStart.AddMonths(1).AddDays(-1));
            }
            case ReportType.QUARTERLY:
            {
                var fyStart = dateInPeriod.StartOfFiscalYear(fiscalYearStartMonth);
                var monthsSinceFyStart = ((dateInPeriod.Year - fyStart.Year) * 12) + (dateInPeriod.Month - fyStart.Month);
                var quarterIndex = monthsSinceFyStart / 3;
                var quarterStart = fyStart.AddMonths(quarterIndex * 3);
                return ($"{FiscalYearLabel(fyStart)}-Q{quarterIndex + 1}", quarterStart, quarterStart.AddMonths(3).AddDays(-1));
            }
            case ReportType.YEARLY:
            {
                var fyStart = dateInPeriod.StartOfFiscalYear(fiscalYearStartMonth);
                return (FiscalYearLabel(fyStart), fyStart, fyStart.AddYears(1).AddDays(-1));
            }
            default:
                return ($"{dateInPeriod:yyyy-MM-dd}", dateInPeriod, dateInPeriod);
        }
    }

    /// <summary>
    /// Builds the fiscal-year label <c>FY&lt;start-year-4-digits&gt;-&lt;end-year-2-digits&gt;</c> for a fiscal year
    /// starting at <paramref name="fyStart"/> (e.g. a July-start fiscal year 2025 → <c>FY2025-26</c>, a January-start
    /// fiscal year 2025 → <c>FY2025-25</c>).
    /// </summary>
    private static string FiscalYearLabel(DateTime fyStart)
    {
        var endYear = fyStart.AddYears(1).AddDays(-1).Year;
        return $"FY{fyStart.Year:D4}-{endYear % 100:D2}";
    }

    private static string FiscalYearLabel(DateTimeOffset fyStart) => FiscalYearLabel(fyStart.Date);

    /// <summary>
    /// Returns the start date of the period that immediately follows the one starting at <paramref name="periodStart"/>.
    /// </summary>
    public static DateTimeOffset NextPeriodStart(ReportType type, DateTimeOffset periodStart) => type switch
    {
        ReportType.WEEKLY => periodStart.AddDays(7),
        ReportType.MONTHLY => periodStart.AddMonths(1),
        ReportType.QUARTERLY => periodStart.AddMonths(3),
        ReportType.YEARLY => periodStart.AddYears(1),
        _ => periodStart,
    };
}
