using Ddth.Utilities.Tempus;
using MyPo.Portfolio.Shared.Models;
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
                var delaySecs = Random.Shared.Next(10 * 60, 20 * 60);
                Logger.LogInformation("Waiting for {delaySecs} seconds before the next execution...", delaySecs);
                await Task.Delay(delaySecs * 1000, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }

    private async Task BuildReportForPortfolio(IServiceScope scope, PortfolioEntity portfolio, CancellationToken cancellationToken)
    {
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

        // resolve the portfolio's timezone from its default market; fall back to UTC when not configured
        var market = Globals.MarketsMap.TryGetValue(metadata.DefaultMarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
        var tz = market?.TZ ?? TimeZoneInfo.Utc;

        await BuildWeeklyReport(scope, portfolio, tz, cancellationToken);
        await BuildMonthlyReport(scope, portfolio, tz, cancellationToken);
        await BuildQuarterlyReport(scope, portfolio, tz, cancellationToken);
        await BuildYearlyReport(scope, portfolio, tz, cancellationToken);
    }

    private Task<bool> BuildWeeklyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.WEEKLY, portfolio.Metadata!.WeeklyReportPeriodStart, cancellationToken);

    private Task<bool> BuildMonthlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.MONTHLY, portfolio.Metadata!.MonthlyReportPeriodStart, cancellationToken);

    private Task<bool> BuildQuarterlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.QUARTERLY, portfolio.Metadata!.QuarterlyReportPeriodStart, cancellationToken);

    private Task<bool> BuildYearlyReport(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, CancellationToken cancellationToken)
        => BuildReportForPeriodType(scope, portfolio, tz, ReportType.YEARLY, portfolio.Metadata!.YearlyReportPeriodStart, cancellationToken);

    /// <summary>
    /// Determines the next report period (of the given <paramref name="type"/>) due for a portfolio, taking
    /// the portfolio's timezone, first-day-of-week and fiscal-year-start-month into account, then logs it.
    /// </summary>
    /// <param name="lastPeriodStartTimestamp">
    /// Unix-seconds marker of the last reported period start for this <paramref name="type"/> (0 when never reported).
    /// </param>
    /// <remarks>
    /// Status rules for the next period (relative to NOW in the portfolio's timezone):
    /// NOW &lt; period start =&gt; nothing to do; period start &lt;= NOW &lt;= period end =&gt; not-final; NOW &gt; period end =&gt; final.
    /// </remarks>
    private async Task<bool> BuildReportForPeriodType(IServiceScope scope, PortfolioEntity portfolio, TimeZoneInfo tz, ReportType type, long lastPeriodStartTimestamp, CancellationToken cancellationToken)
    {
        var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var metadata = portfolio.Metadata!;
        // if (metadata is null)
        // {
        //     Logger.LogWarning("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': metadata is null.", type, portfolio.Id, portfolio.Name);
        //     return false;
        // }
        var firstDayOfWeek = metadata.FirstDayOfWeek!;
        var fiscalStartMonth = metadata.FiscalYearStartMonth!;
        // if (firstDayOfWeek is null || fiscalStartMonth is null)
        // {
        //     Logger.LogWarning("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': FirstDayOfWeek or FiscalYearStartMonth is not set.", type, portfolio.Id, portfolio.Name);
        //     return false;
        // }

        DateTimeOffset nextPeriodStartLocal;
        if (lastPeriodStartTimestamp > 0)
        {
            // already reported before: the next period is the one right after the last reported period
            var lastStartLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(lastPeriodStartTimestamp), tz);
            nextPeriodStartLocal = NextPeriodStart(type, lastStartLocal);
        }
        else
        {
            // never reported before: the first report period is the one containing the first settlement
            var firstSettlement = await portfolioRepository.GetFirstTxSettlementByPortfolioId(portfolio.Id, cancellationToken);
            if (firstSettlement is null)
            {
                Logger.LogInformation("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': no settlement record found.", type, portfolio.Id, portfolio.Name);
                return false;
            }
            var firstSettlementLocal = TimeZoneInfo.ConvertTime(firstSettlement.TxTime, tz);
            nextPeriodStartLocal = StartOfPeriod(type, firstSettlementLocal, firstDayOfWeek!.Value, fiscalStartMonth!.Value);
        }

        var periodEndLocal = NextPeriodStart(type, nextPeriodStartLocal).AddDays(-1);
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

        Logger.LogInformation("Next {reportType} report period for portfolio '{portfolioId}: {portfolioName}': {periodStart:yyyy-MM-dd} - {periodEnd:yyyy-MM-dd}.",
            type, portfolio.Id, portfolio.Name, nextPeriodStartLocal, periodEndLocal);

        if (nowLocal < nextPeriodStartLocal)
        {
            // the next report period has not started yet: nothing to report
            Logger.LogInformation("Skipping {reportType} report for portfolio '{portfolioId}: {portfolioName}': the next period has not started yet.", type, portfolio.Id, portfolio.Name);
            return false;
        }

        // period start <= NOW <= period end => the period is still in progress => the report is not final yet;
        // NOW > period end => the period is over => the report is final.
        var isFinal = nowLocal > periodEndLocal;
        Logger.LogInformation("{reportType} report for portfolio '{portfolioId}: {portfolioName}' is {reportStatus}.", type, portfolio.Id, portfolio.Name, isFinal ? "final" : "not-final");

        // TODO: compute the period's P&L summary via IPortfolioRepository.GetPnlSummaryForPortfolioForPeriodAsync
        // TODO: build & persist the report entity (carry the final / not-final status)
        // TODO: when the report is final, advance the portfolio's last-report timestamp and period-start markers,
        //       then persist the portfolio via IPortfolioRepository.UpdatePortfolioAsync
        // for now, stop after calculating & logging the next report period
        return false;
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
