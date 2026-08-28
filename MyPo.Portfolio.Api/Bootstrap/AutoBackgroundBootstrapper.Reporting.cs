using Ddth.Utilities.Tempus;
using Finhub.Client;
using FinHub.Client.Models.Stocks;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Background task that periodically builds reporting data for portfolios.
/// </summary>
/// <remarks>
/// NOTE: this is a scaffold. The period detection/iteration is implemented, but the actual report
/// building, persistence and portfolio marker advancement are left as <c>// TODO</c>.
/// </remarks>
sealed class BackgroundPortfolioTaskBuildPortfolioReports : BackgroundPortfolioTask
{
    public BackgroundPortfolioTaskBuildPortfolioReports(
            IServiceProvider serviceProvider, ILogger<BackgroundPortfolioTaskBuildPortfolioReports> logger
        ) : base(serviceProvider, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // delay a bit to avoid all instances running at the same time after deployment or restart
        await DelayForRandomInterval(10, 30, "executing background job", cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                try
                {
                    // fetch all available portfolios (there is no "get all portfolios" repository call,
                    // so enumerate users and collect the portfolios they own), then build reports per portfolio.
                    var identityRepository = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
                    var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
                    var allUsers = await identityRepository.GetAllUsersAsync(cancellationToken: cancellationToken);
                    foreach (var user in allUsers)
                    {
                        var portfolios = await portfolioRepository.GetPortfoliosByUserIdAsync(user.Id, cancellationToken);
                        foreach (var portfolio in portfolios)
                        {
                            try
                            {
                                await BuildReportForPortfolio(scope, portfolio, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "An error occurred while building reports for portfolio '{portfolioId}: {portfolioName}'.", portfolio.Id, portfolio.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred while executing the periodic task.");
                }
            }
            await DelayForRandomInterval(1 * 60 * 60, 2 * 60 * 60, cancellationToken: cancellationToken); // delay 1-2 hours before next execution
        }
    }

    private async Task BuildReportForPortfolio(IServiceScope scope, PortfolioEntity portfolio, CancellationToken cancellationToken)
    {
        var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var metadata = portfolio.Metadata;

        // skip container portfolios, inactive portfolios, or portfolios with incomplete reporting config
        if (metadata?.IsContainer ?? false)
        {
            Logger.LogInformation("Skipping reports for portfolio '{portfolioId}: {portfolioName}': it is a container.", portfolio.Id, portfolio.Name);
            return;
        }
        if (!portfolio.IsActive)
        {
            Logger.LogInformation("Skipping reports for portfolio '{portfolioId}: {portfolioName}': it is not active.", portfolio.Id, portfolio.Name);
            return;
        }
        if (metadata?.FirstDayOfWeek is null)
        {
            Logger.LogInformation("Skipping reports for portfolio '{portfolioId}: {portfolioName}': FirstDayOfWeek is not set.", portfolio.Id, portfolio.Name);
            return;
        }
        if (metadata.FiscalYearStartMonth is null)
        {
            Logger.LogInformation("Skipping reports for portfolio '{portfolioId}: {portfolioName}': FiscalYearStartMonth is not set.", portfolio.Id, portfolio.Name);
            return;
        }

        // cache to store stock quote history
        var quoteHistoryCache = new Dictionary<string, HistoryPoint[]>();

        // resolve the portfolio's timezone from its default market; fall back to UTC when not configured
        var market = Globals.MarketsMap.TryGetValue(metadata.DefaultMarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
        var tz = market?.TZ ?? TimeZoneInfo.Utc;
        var nowUTC = DateTimeOffset.UtcNow;

        // // WEEKLY report, covering 1+ year
        var daysTillReportRun = 1;
        if (nowUTC - portfolio.Metadata!.LastWeeklyReportUTC < TimeSpan.FromDays(daysTillReportRun))
        {
            Logger.LogInformation("Skipping WEEKLY report for portfolio '{portfolioId}: {portfolioName}': last report was less than {daysTillReportRun} day(s) ago.", portfolio.Id, portfolio.Name, daysTillReportRun);
        }
        else for (var count = 0; count < 53; count++)
        {
            portfolio.Metadata!.LastWeeklyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var (isFinal, periodStart) = await BuildWeeklyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
            if (isFinal)
            {
                portfolio.Metadata!.WeeklyReportPeriodStart = periodStart.ToUnixTimeSeconds();
                Logger.LogInformation("Updating portfolio metadata 'WeeklyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' to {nextPeriodStart:yyyy-MM-dd HH:mm zzz}.", portfolio.Id, portfolio.Name, portfolio.Metadata!.WeeklyReportPeriodStartUTC);
                var dbresult = await portfolioRepository.UpdatePortfolioAsync(portfolio, cancellationToken);
                if (dbresult is null)
                {
                    Logger.LogWarning("Updating portfolio metadata 'WeeklyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' failed.", portfolio.Id, portfolio.Name);
                    throw new Exception($"Updating portfolio metadata 'WeeklyReportPeriodStart' for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
                }
            }
            else break;
        }

        // MONTHLY report, covering 1+ year
        daysTillReportRun = 3;
        if (nowUTC - portfolio.Metadata!.LastMonthlyReportUTC < TimeSpan.FromDays(daysTillReportRun))
        {
            Logger.LogInformation("Skipping MONTHLY report for portfolio '{portfolioId}: {portfolioName}': last report was less than {daysTillReportRun} day(s) ago.", portfolio.Id, portfolio.Name, daysTillReportRun);
        }
        else for (var count = 0; count < 13; count++)
        {
            portfolio.Metadata!.LastMonthlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var (isFinal, periodStart) = await BuildMonthlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
            if (isFinal)
            {
                portfolio.Metadata!.MonthlyReportPeriodStart = periodStart.ToUniversalTime().ToUnixTimeSeconds();
                Logger.LogInformation("Updating portfolio metadata 'MonthlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' to {nextPeriodStart:yyyy-MM-dd HH:mm zzz}.", portfolio.Id, portfolio.Name, portfolio.Metadata!.MonthlyReportPeriodStartUTC);
                var dbresult = await portfolioRepository.UpdatePortfolioAsync(portfolio, cancellationToken);
                if (dbresult is null)
                {
                    Logger.LogWarning("Updating portfolio metadata 'MonthlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' failed.", portfolio.Id, portfolio.Name);
                    throw new Exception($"Updating portfolio metadata 'MonthlyReportPeriodStart' for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
                }
            }
            else break;
        }

        // QUARTERLY report, covering 1+ year
        daysTillReportRun = 5;
        if (nowUTC - portfolio.Metadata!.LastQuarterlyReportUTC < TimeSpan.FromDays(daysTillReportRun))
        {
            Logger.LogInformation("Skipping QUARTERLY report for portfolio '{portfolioId}: {portfolioName}': last report was less than {daysTillReportRun} day(s) ago.", portfolio.Id, portfolio.Name, daysTillReportRun);
        }
        else for (var count = 0; count < 5; count++)
        {
            portfolio.Metadata!.LastQuarterlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var (isFinal, periodStart) = await BuildQuarterlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
            if (isFinal)
            {
                portfolio.Metadata!.QuarterlyReportPeriodStart = periodStart.ToUniversalTime().ToUnixTimeSeconds();
                Logger.LogInformation("Updating portfolio metadata 'QuarterlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' to {nextPeriodStart:yyyy-MM-dd HH:mm zzz}.", portfolio.Id, portfolio.Name, portfolio.Metadata!.QuarterlyReportPeriodStartUTC);
                var dbresult = await portfolioRepository.UpdatePortfolioAsync(portfolio, cancellationToken);
                if (dbresult is null)
                {
                    Logger.LogWarning("Updating portfolio metadata 'QuarterlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' failed.", portfolio.Id, portfolio.Name);
                    throw new Exception($"Updating portfolio metadata 'QuarterlyReportPeriodStart' for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
                }
            }
            else break;
        }

        // YEARLY report, covering 1+ year
        daysTillReportRun = 7;
        if (nowUTC - portfolio.Metadata!.LastYearlyReportUTC < TimeSpan.FromDays(daysTillReportRun))
        {
            Logger.LogInformation("Skipping YEARLY report for portfolio '{portfolioId}: {portfolioName}': last report was less than {daysTillReportRun} day(s) ago.", portfolio.Id, portfolio.Name, daysTillReportRun);
        }
        else for (var count = 0; count < 2; count++)
        {
            portfolio.Metadata!.LastYearlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var (isFinal, periodStart) = await BuildYearlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
            if (isFinal)
            {
                portfolio.Metadata!.YearlyReportPeriodStart = periodStart.ToUniversalTime().ToUnixTimeSeconds();
                Logger.LogInformation("Updating portfolio metadata 'YearlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' to {nextPeriodStart:yyyy-MM-dd HH:mm zzz}.", portfolio.Id, portfolio.Name, portfolio.Metadata!.YearlyReportPeriodStartUTC);
                var dbresult = await portfolioRepository.UpdatePortfolioAsync(portfolio, cancellationToken);
                if (dbresult is null)
                {
                    Logger.LogWarning("Updating portfolio metadata 'YearlyReportPeriodStart' for portfolio '{portfolioId}: {portfolioName}' failed.", portfolio.Id, portfolio.Name);
                    throw new Exception($"Updating portfolio metadata 'YearlyReportPeriodStart' for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
                }
            }
        }

        // finally update the portfolio's last-report-timestamp
        {
            var dbresult = await portfolioRepository.UpdatePortfolioAsync(portfolio, cancellationToken);
            if (dbresult is null)
            {
                Logger.LogWarning("Updating portfolio metadata for portfolio '{portfolioId}: {portfolioName}' failed.", portfolio.Id, portfolio.Name);
                throw new Exception($"Updating portfolio metadata for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
            }
        }
    }

    private Task<(bool, DateTimeOffset)> BuildWeeklyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, IDictionary<string, HistoryPoint[]> quoteHistoryCache, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.WEEKLY, portfolio.Metadata!.WeeklyReportPeriodStart, quoteHistoryCache, cancellationToken);

    private Task<(bool, DateTimeOffset)> BuildMonthlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, IDictionary<string, HistoryPoint[]> quoteHistoryCache, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.MONTHLY, portfolio.Metadata!.MonthlyReportPeriodStart, quoteHistoryCache, cancellationToken);

    private Task<(bool, DateTimeOffset)> BuildQuarterlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, IDictionary<string, HistoryPoint[]> quoteHistoryCache, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.QUARTERLY, portfolio.Metadata!.QuarterlyReportPeriodStart, quoteHistoryCache, cancellationToken);

    private Task<(bool, DateTimeOffset)> BuildYearlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, IDictionary<string, HistoryPoint[]> quoteHistoryCache, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.YEARLY, portfolio.Metadata!.YearlyReportPeriodStart, quoteHistoryCache, cancellationToken);

    /// <summary>
    /// Determines the next report period (of the given <paramref name="reportType"/>) due for a portfolio, taking
    /// the portfolio's timezone, first-day-of-week and fiscal-year-start-month into account, then logs it.
    /// </summary>
    /// <param name="lastPeriodStartTimestamp">
    /// Unix-seconds marker of the last reported period start for this <paramref name="reportType"/> - as UTC timestamp (0 when never reported).
    /// </param>
    /// <remarks>
    /// Status rules for the next period (relative to NOW in the portfolio's timezone):
    /// NOW &lt; period start =&gt; nothing to do; period start &lt;= NOW &lt;= period end =&gt; not-final; NOW &gt; period end =&gt; final.
    /// </remarks>
    private async Task<(bool, DateTimeOffset)> BuildReportForPeriodType(
        IServiceScope scope,
        PortfolioEntity portfolio,
        TimeZoneInfo tz,
        ReportType reportType,
        long lastPeriodStartTimestamp,
        IDictionary<string, HistoryPoint[]> quoteHistoryCache,
        CancellationToken cancellationToken)
    {
        var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
        var metadata = portfolio.Metadata!;
        var firstDayOfWeek = metadata.FirstDayOfWeek!;
        var fiscalStartMonth = metadata.FiscalYearStartMonth!;

        DateTimeOffset nextPeriodStartLocal, periodEndLocal;
        if (lastPeriodStartTimestamp > 0)
        {
            // already reported before: the next period is the one right after the last reported period
            var lastStartLocal = DateTimeOffset.FromUnixTimeSeconds(lastPeriodStartTimestamp).ToTimeZoneSilently(tz.Id)!.Value;
            nextPeriodStartLocal = ReportUtils.NextPeriodStart(reportType, lastStartLocal);
            (_, nextPeriodStartLocal, periodEndLocal) = ReportUtils.ComputePeriod(reportType, nextPeriodStartLocal, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
        }
        else
        {
            // never reported before: the first report period is the one containing the first settlement
            var firstSettlement = await portfolioRepository.GetFirstTxSettlementByPortfolioId(portfolio.Id, cancellationToken);
            if (firstSettlement is null)
            {
                Logger.LogInformation("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': no settlement record found.", reportType, portfolio.Id, portfolio.Name);
                return (false, DateTimeOffset.MinValue);
            }
            var firstSettlementLocal = firstSettlement.TxTime.ToTimeZoneSilently(tz.Id)!.Value;
            (_, nextPeriodStartLocal, periodEndLocal) = ReportUtils.ComputePeriod(reportType, firstSettlementLocal, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
        }

        var nowLocal = DateTimeOffset.UtcNow.ToTimeZoneSilently(tz.Id)!.Value;

        Logger.LogInformation("Next {reportType} report period for portfolio '{portfolioId}: {portfolioName}': {periodStart:yyyy-MM-dd HH:mm zzz} - {periodEnd:yyyy-MM-dd HH:mm zzz}.",
            reportType, portfolio.Id, portfolio.Name, nextPeriodStartLocal, periodEndLocal);

        if (nowLocal < nextPeriodStartLocal)
        {
            // the next report period has not started yet: nothing to report
            Logger.LogInformation("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': the next period has not started yet.", reportType, portfolio.Id, portfolio.Name);
            return (false, DateTimeOffset.MinValue);
        }

        var pnlSummaryList = (await portfolioRepository.GetPnlSummaryForPortfolioForPeriodAsync(portfolio.Id, nextPeriodStartLocal, periodEndLocal, cancellationToken)).ToList();
        var reportEntries = new List<ReportEntity>();

        // first round: build item report entries
        foreach (var pnlSummary in pnlSummaryList)
        {
            var report = await ReportUtils.BuildItemReport(
                pnlSummary, Globals.Markets, reportType, nextPeriodStartLocal,
                portfolioRepository, finHubClient, quoteHistoryCache,
                firstDayOfWeek!.Value, fiscalStartMonth!.Value);
            if (report is not null) reportEntries.Add(report);
        }

        // carry-forward round for held-but-not-traded positions
        // For every open position not traded this period, synthesize a mark-to-market POSITION row
        // so it appears in the holdings breakdown AND is included in the portfolio CloseValue sum below.
        var tradedCodes = reportEntries
            .Where(r => r.ItemCode != ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO)
            .Select(r => r.ItemCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var openPositions = await portfolioRepository.GetOpenPositionsAsOfAsync(portfolio, reportType, nextPeriodStartLocal.ToString("yyyy-MM-dd"), cancellationToken);
        foreach (var pos in openPositions.Where(p => !tradedCodes.Contains(p.ItemCode)))
        {
            // synthesize a zero-activity PnlSummary and run BuildItemReport to mark-to-market
            // #1: Create zero-activity PnlSummary
            var zeroSummary = PnlSummary.New(portfolio.Id);
            zeroSummary.RefItemCode = pos.ItemCode;
            // #2: Build carried forward entry
            var carried = await ReportUtils.BuildItemReport(
                zeroSummary, Globals.Markets, reportType, nextPeriodStartLocal,
                portfolioRepository, finHubClient, quoteHistoryCache,
                firstDayOfWeek!.Value, fiscalStartMonth!.Value);
            if (carried is not null) reportEntries.Add(carried);
        }

        // second round: build portfolio level report
        foreach (var pnlSummary in pnlSummaryList)
        {
            var report = await ReportUtils.BuildPortfolioReport(
                pnlSummary, Globals.Markets, reportType,
                nextPeriodStartLocal, portfolioRepository, reportEntries,
                firstDayOfWeek!.Value, fiscalStartMonth!.Value);
            if (report is not null) reportEntries.Add(report);
        }

        if (reportEntries.Count > 0)
        {
            var dbresult = await portfolioRepository.SaveReportsAsync(reportEntries, cancellationToken);
            if (!dbresult)
            {
                Logger.LogWarning("Saving reports for {reportType} report period for portfolio '{portfolioId}: {portfolioName}' failed.", reportType, portfolio.Id, portfolio.Name);
                throw new Exception($"Saving reports for {reportType} report period for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
            }
        }
        else
        {
            Logger.LogWarning("Empty {reportType} report period for portfolio '{portfolioId}: {portfolioName}'.", reportType, portfolio.Id, portfolio.Name);
        }

        // period start <= NOW < period end ==> the period is still in progress => the report is not final yet;
        // period end <= NOW ==> the period is over => the report is final.
        var isFinal = periodEndLocal <= nowLocal;
        Logger.LogInformation("{reportType} report for portfolio '{portfolioId}: {portfolioName}' is {reportStatus}.", reportType, portfolio.Id, portfolio.Name, isFinal ? "final" : "not-final");

        return (isFinal, nextPeriodStartLocal);
    }
}
