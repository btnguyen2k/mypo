using System.Text.Json;
using Ddth.Utilities.Tempus;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
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
sealed class AutoBackgroundReporting : AutoBackgroundAnnouncementScanner
{
    public AutoBackgroundReporting(
            IServiceProvider serviceProvider, ILogger<AutoBackgroundReporting> logger
        ) : base(serviceProvider, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // delay a bit to avoid all instances running at the same time after deployment or restart
        await Task.Delay(Random.Shared.Next(10000, 30000), cancellationToken);

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
            try
            {
                var delaySecs = Random.Shared.Next(60 * 60, 90 * 60);
                Logger.LogInformation("Waiting for {delaySecs} seconds before the next execution...", delaySecs);
                await Task.Delay(delaySecs * 1000, cancellationToken);
            }
            catch (TaskCanceledException) { }
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

        var quoteHistoryCache = new Dictionary<string, HistoryPoint[]>();

        // resolve the portfolio's timezone from its default market; fall back to UTC when not configured
        var market = Globals.MarketsMap.TryGetValue(metadata.DefaultMarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
        var tz = market?.TZ ?? TimeZoneInfo.Utc;
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();

        // WEEKLY report, covering 1 year
        if (now - portfolio.Metadata!.LastWeeklyReportTimestamp < TimeSpan.FromDays(1).TotalSeconds)
        {
            Logger.LogInformation("Skipping weekly report for portfolio '{portfolioId}: {portfolioName}': last report was less than 1 day ago.", portfolio.Id, portfolio.Name);
        }
        else
        {
            for (var count = 0; count < 53; count++)
            {
                portfolio.Metadata!.LastWeeklyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var (isFinal, nextPeriodStart) = await BuildWeeklyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
                if (isFinal)
                {
                    portfolio.Metadata!.WeeklyReportPeriodStart = nextPeriodStart.ToUnixTimeSeconds();
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
        }

        // MONTHLY report, covering 1 year
        if (now - portfolio.Metadata!.LastMonthlyReportTimestamp < TimeSpan.FromDays(3).TotalSeconds)
        {
            Logger.LogInformation("Skipping monthly report for portfolio '{portfolioId}: {portfolioName}': last report was less than 3 days ago.", portfolio.Id, portfolio.Name);
        }
        else
        {
            for (var count = 0; count < 12; count++)
            {
                portfolio.Metadata!.LastMonthlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var (isFinal, nextPeriodStart) = await BuildMonthlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
                if (isFinal)
                {
                    portfolio.Metadata!.MonthlyReportPeriodStart = nextPeriodStart.ToUnixTimeSeconds();
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
        }

        // QUARTERLY report, covering 1 year
        if (now - portfolio.Metadata!.LastQuarterlyReportTimestamp < TimeSpan.FromDays(5).TotalSeconds)
        {
            Logger.LogInformation("Skipping quarterly report for portfolio '{portfolioId}: {portfolioName}': last report was less than 5 days ago.", portfolio.Id, portfolio.Name);
        }
        else
        {
            for (var count = 0; count < 4; count++)
            {
                portfolio.Metadata!.LastQuarterlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var (isFinal, nextPeriodStart) = await BuildQuarterlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
                if (isFinal)
                {
                    portfolio.Metadata!.QuarterlyReportPeriodStart = nextPeriodStart.ToUnixTimeSeconds();
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
        }

        // YEARLY report, covering 1 year
        if (now - portfolio.Metadata!.LastYearlyReportTimestamp < TimeSpan.FromDays(6).TotalSeconds)
        {
            Logger.LogInformation("Skipping yearly report for portfolio '{portfolioId}: {portfolioName}': last report was less than 6 days ago.", portfolio.Id, portfolio.Name);
        }
        else
        {
            portfolio.Metadata!.LastYearlyReportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var (isFinal, nextPeriodStart) = await BuildYearlyReport(scope, portfolio, tz, quoteHistoryCache, cancellationToken);
            if (isFinal)
            {
                portfolio.Metadata!.YearlyReportPeriodStart = nextPeriodStart.ToUnixTimeSeconds();
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
    /// Unix-seconds marker of the last reported period start for this <paramref name="reportType"/> (0 when never reported).
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

        DateTimeOffset nextPeriodStartLocal;
        if (lastPeriodStartTimestamp > 0)
        {
            // already reported before: the next period is the one right after the last reported period
            var lastStartLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(lastPeriodStartTimestamp), tz);
            nextPeriodStartLocal = NextPeriodStart(reportType, lastStartLocal);
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
            var firstSettlementLocal = TimeZoneInfo.ConvertTime(firstSettlement.TxTime, tz);
            nextPeriodStartLocal = StartOfPeriod(reportType, firstSettlementLocal, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
        }

        var periodEndLocal = NextPeriodStart(reportType, nextPeriodStartLocal).AddDays(-1);
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

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
            var report = await ReportUtils.BuildItemReport(pnlSummary, Globals.Markets, reportType, nextPeriodStartLocal, portfolioRepository, finHubClient, quoteHistoryCache, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
            if (report is not null) reportEntries.Add(report);
        }
        // second round: build portfolio level report
        foreach (var pnlSummary in pnlSummaryList)
        {
            var report = await ReportUtils.BuildPortfolioReport(pnlSummary, Globals.Markets, reportType, nextPeriodStartLocal, portfolioRepository, reportEntries, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
            if (report is not null) reportEntries.Add(report);
        }

        var dbresult = await portfolioRepository.SaveReportsAsync(reportEntries, cancellationToken);
        if (!dbresult)
        {
            Logger.LogWarning("Saving reports for {reportType} report period for portfolio '{portfolioId}: {portfolioName}' failed.", reportType, portfolio.Id, portfolio.Name);
            throw new Exception($"Saving reports for {reportType} report period for portfolio '{portfolio.Id}: {portfolio.Name}' failed.");
        }

        // period start <= NOW < period end ==> the period is still in progress => the report is not final yet;
        // period end <= NOW ==> the period is over => the report is final.
        var isFinal = periodEndLocal <= nowLocal;
        Logger.LogInformation("{reportType} report for portfolio '{portfolioId}: {portfolioName}' is {reportStatus}.", reportType, portfolio.Id, portfolio.Name, isFinal ? "final" : "not-final");

        return (isFinal, nextPeriodStartLocal);
    }

    /// <summary>
    /// Returns the start date of the period (of <paramref name="type"/>) that contains <paramref name="localDate"/>.
    /// </summary>
    private static DateTimeOffset StartOfPeriod(ReportType type, DateTimeOffset localDate, DayOfWeek firstDayOfWeek, int fiscalStartMonth) => type switch
    {
        ReportType.WEEKLY => localDate.StartOfWeek(firstDayOfWeek),
        ReportType.MONTHLY => localDate.StartOfMonth(),
        ReportType.QUARTERLY => localDate.StartOfQuarter(),
        ReportType.YEARLY => localDate.StartOfFiscalYear(fiscalStartMonth),
        _ => localDate,
    };

    /// <summary>
    /// Returns the start date of the period that immediately follows the one starting at <paramref name="periodStart"/>.
    /// </summary>
    private static DateTimeOffset NextPeriodStart(ReportType type, DateTimeOffset periodStart) => type switch
    {
        ReportType.WEEKLY => periodStart.AddDays(7),
        ReportType.MONTHLY => periodStart.AddMonths(1),
        ReportType.QUARTERLY => periodStart.AddMonths(3),
        ReportType.YEARLY => periodStart.AddYears(1),
        _ => periodStart,
    };
}
