using System.Globalization;
using Ddth.Utilities.Tempus;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
    /// <summary>
    /// Gets report periods for a given portfolio of a given type.
    /// </summary>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_REPORT_PERIODS)]
    public async ValueTask<ActionResult<ApiResp<IEnumerable<string>>>> GetReportPeriods([FromRoute] string type, [FromQuery] string pid)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingPortfolio = await GetPortfolioIfAccessible(currentUser, pid);
        if (existingPortfolio == null)
        {
            return ResponseNoData(404, "Portfolio not found.");
        }

        // check if type is one of WEEKLY, MONTHLY, QUARTERLY, YEARLY; return error 400 if invalid.
        if (!Enum.TryParse<ReportType>(type, ignoreCase: true, out var reportType))
        {
            return ResponseNoData(400, $"Invalid report type '{type}'. Valid types are: WEEKLY, MONTHLY, QUARTERLY, YEARLY.");
        }

        // obtain portfolio's default market and its timezone (fall back to UTC when not configured)
        var market = Globals.MarketsMap.TryGetValue(existingPortfolio.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
        var tz = market?.TZ ?? TimeZoneInfo.Utc;

        // call IPortfolioRepository.GetFirstTxSettlementByPortfolioId to get the first settlement record for the portfolio; return empty list if not found.
        var firstSettlement = await PortfolioRepository.GetFirstTxSettlementByPortfolioId(pid);
        if (firstSettlement == null)
        {
            return ResponseOk(Enumerable.Empty<string>());
        }

        // calculate report periods based on the first settlement date, current date, and report type;
        // take the market timezone into consideration when bucketing dates into periods.
        var firstDayOfWeek = existingPortfolio.Metadata?.FirstDayOfWeek ?? DayOfWeek.Monday;
        var fiscalStartMonth = existingPortfolio.Metadata?.FiscalYearStartMonth ?? 1;
        var startLocal = TimeZoneInfo.ConvertTime(firstSettlement.TxTime, tz).Date;
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

        var periods = BuildReportPeriods(reportType, startLocal, nowLocal, firstDayOfWeek, fiscalStartMonth);
        return ResponseOk(periods);
    }

    /// <summary>
    /// Builds the list of report period identifiers (newest first) spanning the first settlement date
    /// up to the current date, formatted per <see cref="ReportEntity.Period"/> (ISO 8601 style).
    /// </summary>
    private static List<string> BuildReportPeriods(ReportType type, DateTime startLocal, DateTime nowLocal, DayOfWeek firstDayOfWeek, int fiscalStartMonth)
    {
        var periods = new List<string>();
        switch (type)
        {
            case ReportType.WEEKLY:
            {
                var cursor = startLocal.StartOfWeek(firstDayOfWeek);
                var last = nowLocal.StartOfWeek(firstDayOfWeek);
                while (cursor <= last)
                {
                    var weekEnd = cursor.AddDays(6);
                    periods.Add($"{ISOWeek.GetYear(cursor):D4}-W{ISOWeek.GetWeekOfYear(cursor):D2}: {cursor:yyyy-MM-dd} - {weekEnd:yyyy-MM-dd}");
                    cursor = cursor.AddDays(7);
                }
                break;
            }
            case ReportType.MONTHLY:
            {
                var cursor = startLocal.StartOfMonth();
                var last = nowLocal.StartOfMonth();
                while (cursor <= last)
                {
                    periods.Add($"{cursor:yyyy-MM}");
                    cursor = cursor.AddMonths(1);
                }
                break;
            }
            case ReportType.QUARTERLY:
            {
                var cursor = startLocal.StartOfQuarter();
                var last = nowLocal.StartOfQuarter();
                while (cursor <= last)
                {
                    var fyStart = cursor.StartOfFiscalYear(fiscalStartMonth);
                    var quarterNum = (((cursor.Year - fyStart.Year) * 12) + (cursor.Month - fyStart.Month)) / 3 + 1;
                    var quarterEnd = cursor.AddMonths(2);
                    periods.Add($"FY{fyStart.Year % 100:D2}-Q{quarterNum}: {cursor:yyyy-MM} - {quarterEnd:yyyy-MM}");
                    cursor = cursor.AddMonths(3);
                }
                break;
            }
            case ReportType.YEARLY:
            {
                var cursor = startLocal.StartOfFiscalYear(fiscalStartMonth);
                var last = nowLocal.StartOfFiscalYear(fiscalStartMonth);
                while (cursor <= last)
                {
                    var yearEnd = cursor.AddMonths(11);
                    periods.Add($"FY{cursor.Year % 100:D2}: {cursor:yyyy-MM} - {yearEnd:yyyy-MM}");
                    cursor = cursor.AddYears(1);
                }
                break;
            }
        }
        periods.Reverse(); // newest period first
        return periods;
    }
}
