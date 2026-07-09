using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
    [HttpPost(IPortfolioApiClient.API_REPORT_RESET)]
    public async ValueTask<ActionResult<ApiResp>> ResetReports([FromRoute] string portfolioId)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, portfolioId);
        if (existingPortfolio == null)
        {
            return ResponseNoData(404, "Portfolio not found.");
        }

        await PortfolioRepository.ResetReports(portfolioId);
        return ResponseOk();
    }

    /// <summary>
    /// Gets report periods for a given portfolio of a given type.
    /// </summary>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_REPORT_PERIODS)]
    public async ValueTask<ActionResult<ApiResp<IEnumerable<ReportPeriod>>>> GetReportPeriods([FromRoute] string type, [FromQuery] string pid)
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
            return ResponseOk(Enumerable.Empty<ReportPeriod>());
        }

        // build the report periods (newest first) spanning the first settlement date up to the current date,
        // reusing ReportUtils so the period labels stay consistent with the generated report entries.
        var firstDayOfWeek = existingPortfolio.Metadata?.FirstDayOfWeek ?? DayOfWeek.Monday;
        var fiscalStartMonth = existingPortfolio.Metadata?.FiscalYearStartMonth ?? 1;
        var startLocal = TimeZoneInfo.ConvertTime(firstSettlement.TxTime, tz).Date;
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

        var periods = ReportUtils.GenerateReportPeriods(reportType, startLocal, nowLocal, firstDayOfWeek, fiscalStartMonth)
            .Select(p => new ReportPeriod
            {
                Type = reportType,
                Start = p.Start.ToString("yyyy-MM-dd"),
                Label = $"{p.Label}: {p.Start:yyyy-MM-dd} to {p.End:yyyy-MM-dd}",
            })
            .Reverse()
            .ToList();
        return ResponseOk(periods);
    }

    /// <summary>
    /// Gets report snapshot for a given portfolio of a given type and period start date.
    /// </summary>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_REPORT_SNAPSHOT)]
    public async ValueTask<ActionResult<ApiResp<IEnumerable<ReportEntity>>>> GetReportSnapshot([FromRoute] string type, [FromQuery] string pid, [FromQuery] string start, [FromQuery] string symbol)
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

        // // validate the period start date; it must be in the yyyy-MM-dd format.
        // if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodStart))
        // {
        //     return ResponseNoData(400, $"Invalid period start '{start}'. Expected format is yyyy-MM-dd.");
        // }

        // fetch the report snapshot from the database; the repository normalizes an empty symbol to the
        // whole-portfolio marker ("*") on our behalf.
        // var reportPeriod = periodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var snapshot = await PortfolioRepository.GetSnapshotReportAsync(existingPortfolio, reportType, start, symbol);
        return ResponseOk(snapshot);
    }
}
