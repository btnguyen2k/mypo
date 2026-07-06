using Ddth.Utilities.Tempus;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Utils;

public static class ReportUtils
{
    /// <summary>
    /// Builds the list of per-transaction-type report entries for the given PnL summary, report type, and report period.
    /// One entry is produced for each transaction type (BUY, SELL, TAX, DIVIDEND, DISTRIBUTION) that has a positive value.
    /// </summary>
    /// <param name="pnlSummary">The aggregated PnL summary to derive the entries from.</param>
    /// <param name="markets">The list of market definitions, used to build the full item codes.</param>
    /// <param name="reportType">The type of the report period (weekly/monthly/quarterly/yearly).</param>
    /// <param name="reportPeriod">Any point in time within the target period; it is normalized to the period's start.</param>
    /// <param name="weekStartDay">The first day of the week (used for weekly periods).</param>
    /// <param name="fiscalYearStartMonth">The first month (1-12) of the fiscal year (used for weekly/quarterly/yearly periods).</param>
    /// <returns>A report entry per transaction type with a positive value.</returns>
    public static IEnumerable<ReportEntity> BuildReportEntries(PnlSummary pnlSummary, IEnumerable<MarketDef> markets, ReportType reportType, DateTimeOffset reportPeriod, DayOfWeek weekStartDay = DayOfWeek.Monday, int fiscalYearStartMonth = 1)
    {
        reportPeriod = reportPeriod.ToUniversalTime();
        var marketMap = markets.ToDictionary(m => m.Id, m => m.Code.ToUpper());
        var market = marketMap.TryGetValue(pnlSummary.RefMarketId??string.Empty, out var marketCode) ? marketCode : null;
        var (label, start, end) = ComputePeriod(reportType, reportPeriod.Date, weekStartDay, fiscalYearStartMonth);
        var periodStart = start.ToString("yyyy-MM-dd");
        var itemCode = string.IsNullOrEmpty(pnlSummary.RefItemCode) ? "*" : $"{(market is null ? string.Empty : market+":")}{pnlSummary.RefItemCode}";
        var isFinal = end <= DateTime.UtcNow;

        ReportEntity Entry(string txType, decimal quantity, decimal cost)
        {
            return new ReportEntity
            {
                Type = reportType,
                PeriodStart = periodStart,
                PeriodLabel = label,
                PortfolioId = pnlSummary.PortfolioId,
                ItemCode = itemCode,
                TxType = txType,
                Quantity = quantity,
                Cost = cost,
                IsFinal = isFinal,
            };
        }

        var candidates = new (decimal Cost, string TxType, decimal Quantity)[]
        {
            (pnlSummary.TotalBuyValue, TxSettlementEntity.TX_TYPE_BUY, pnlSummary.TotalBuyQuantity),
            (pnlSummary.TotalSellValue, TxSettlementEntity.TX_TYPE_SELL, pnlSummary.TotalSellQuantity),
            (pnlSummary.TotalTax, TxSettlementEntity.TX_TYPE_TAX, 0),
            (pnlSummary.TotalFees, TxSettlementEntity.TX_TYPE_FEE, 0),
            (pnlSummary.TotalInterest, TxSettlementEntity.TX_TYPE_INTEREST, 0),
            (pnlSummary.TotalDividends, TxSettlementEntity.TX_TYPE_DIVIDEND, 0),
            (pnlSummary.TotalDistributions, TxSettlementEntity.TX_TYPE_DISTRIBUTION, 0),
            (pnlSummary.TotalCashIn, TxSettlementEntity.TX_TYPE_CASHIN, 0),
            (pnlSummary.TotalCashOut, TxSettlementEntity.TX_TYPE_CASHOUT, 0),
        };

        return [.. candidates
            .Where(c => c.Cost > 0)
            .Select(c => Entry(c.TxType, c.Quantity, c.Cost))];
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
    /// Computes the short label and the inclusive start/end dates of the report period containing <paramref name="date"/>.
    /// </summary>
    private static (string Label, DateTime Start, DateTime End) ComputePeriod(ReportType reportType, DateTime date, DayOfWeek weekStart, int fiscalYearStartMonth)
    {
        switch (reportType)
        {
            case ReportType.WEEKLY:
            {
                var fyStart = date.StartOfFiscalYear(fiscalYearStartMonth);
                var week1Start = fyStart.StartOfWeek(weekStart);
                var weekStartDate = date.StartOfWeek(weekStart);
                var weekNumber = (int)((weekStartDate - week1Start).TotalDays / 7) + 1;
                return ($"{FiscalYearLabel(fyStart)}-W{weekNumber:D2}", weekStartDate, weekStartDate.AddDays(6));
            }
            case ReportType.MONTHLY:
            {
                var monthStart = new DateTime(date.Year, date.Month, 1);
                return ($"{monthStart:yyyy-MM}", monthStart, monthStart.AddMonths(1).AddDays(-1));
            }
            case ReportType.QUARTERLY:
            {
                var fyStart = date.StartOfFiscalYear(fiscalYearStartMonth);
                var monthsSinceFyStart = ((date.Year - fyStart.Year) * 12) + (date.Month - fyStart.Month);
                var quarterIndex = monthsSinceFyStart / 3;
                var quarterStart = fyStart.AddMonths(quarterIndex * 3);
                return ($"{FiscalYearLabel(fyStart)}-Q{quarterIndex + 1}", quarterStart, quarterStart.AddMonths(3).AddDays(-1));
            }
            case ReportType.YEARLY:
            {
                var fyStart = date.StartOfFiscalYear(fiscalYearStartMonth);
                return (FiscalYearLabel(fyStart), fyStart, fyStart.AddYears(1).AddDays(-1));
            }
            default:
                return ($"{date:yyyy-MM-dd}", date, date);
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
}
