using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Background task that periodically auto-updates (refreshes the holdings of) users' portfolio plans.
/// The cadence is controlled per user by <see cref="PortfolioPlanPreferences.AutoUpdateDays"/>.
/// </summary>
sealed class AutoBackgroundUpdatePortfolioPlansScanner : AutoBackgroundAnnouncementScanner
{
    public AutoBackgroundUpdatePortfolioPlansScanner(
            IServiceProvider serviceProvider, ILogger<AutoBackgroundUpdatePortfolioPlansScanner> logger
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
                    var identityRepository = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
                    var allUsers = await identityRepository.GetAllUsersAsync(cancellationToken: cancellationToken);
                    foreach (var user in allUsers)
                    {
                        try
                        {
                            await UpdatePortfolioPlansForUser(scope, user, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "An error occurred while auto-updating portfolio plans for user '{userId}'", user.Id);
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

    private async Task UpdatePortfolioPlansForUser(IServiceScope scope, MyPoUser user, CancellationToken cancellationToken)
    {
        var userName = user.UserName!.ToLower();
        var prefs = user.Metadata?.GetPortfolioPlanPreferences();
        if (user.Metadata is null || prefs is null || prefs.AutoUpdateDays <= 0)  // auto-update disabled
        {
            return;
        }

        Logger.LogInformation("Auto-updating portfolio plans for user {userName}...", userName);
        var updateDelay = TimeSpan.FromDays(prefs.AutoUpdateDays);
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var holdingsService = scope.ServiceProvider.GetRequiredService<IPortfolioPlanHoldingsService>();
        var plans = await portfolioRepo.GetPortfolioPlansByOwnerUserIdAsync(user.Id, cancellationToken);
        foreach (var plan in plans)
        {
            var lastRefresh = plan.Metadata?.HoldingsRefreshTimestamp ?? 0;
            if (lastRefresh > 0 && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(lastRefresh) < updateDelay)
            {
                // this plan is not due for an auto-update yet
                continue;
            }

            try
            {
                await RefreshPortfolioPlanHoldings(holdingsService, portfolioRepo, plan, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to auto-update portfolio plan '{planId}: {planName}' for user '{userName}'.", plan.Id, plan.Name, userName);
            }
        }
    }

    /// <summary>
    /// Rebuilds a plan's holding tickers (market price, dividend info, and shares/avg-price from any linked
    /// portfolio) via the shared <see cref="IPortfolioPlanHoldingsService"/> and persists it. On a fetch
    /// failure the existing holding values are kept (the service reports the failed tickers).
    /// </summary>
    private async Task RefreshPortfolioPlanHoldings(IPortfolioPlanHoldingsService holdingsService, IPortfolioRepository portfolioRepo, PortfolioPlanEntity plan, CancellationToken cancellationToken)
    {
        var existingHoldings = plan.Metadata?.HoldingTickers ?? [];
        if (existingHoldings.Count == 0)
        {
            return;
        }

        var result = await holdingsService.RefreshHoldingsAsync(existingHoldings, plan.PortfolioId, cancellationToken);
        foreach (var failedTicker in result.FailedTickers)
        {
            Logger.LogWarning("Cannot fetch info for ticker '{ticker}' while updating plan '{planId}'; keeping existing values.", failedTicker, plan.Id);
        }

        plan.Metadata ??= new PortfolioPlanMetadata();
        plan.Metadata.HoldingsRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        plan.Metadata.HoldingTickers = result.Holdings;
        var dbresult = await portfolioRepo.UpdatePortfolioPlanAsync(plan, cancellationToken);
        if (dbresult == null)
        {
            Logger.LogError("Failed to persist auto-updated portfolio plan '{planId}'.", plan.Id);
        }
    }
}
