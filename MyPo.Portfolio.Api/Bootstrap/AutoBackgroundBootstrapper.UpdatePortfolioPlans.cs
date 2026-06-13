using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;
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

        var checkpoint = await GetOrInitCheckpoint(
            ownerId: userName,
            portfolioId: CheckpointEntity.NON_PORTFOLIO,
            marketId: CheckpointEntity.NON_MARKET,
            itemCode: CheckpointEntity.NON_ITEM,
            checkpointType: CheckpointEntity.CHECKPOINT_PORTFOLIO_PLAN_UPDATE,
            cancellationToken
        );
        var updateDelay = TimeSpan.FromDays(prefs.AutoUpdateDays);
        if (checkpoint is null || (checkpoint.CheckpointTime != DateTimeOffset.MinValue && DateTimeOffset.UtcNow - checkpoint.CheckpointTime < updateDelay))
        {
            // not due for an auto-update yet
            return;
        }

        Logger.LogInformation("Auto-updating portfolio plans for user {userName}...", userName);
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
        var plans = await portfolioRepo.GetPortfolioPlansByOwnerUserIdAsync(user.Id, cancellationToken);
        foreach (var plan in plans)
        {
            try
            {
                await RefreshPortfolioPlanHoldings(portfolioRepo, finHubClient, plan, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to auto-update portfolio plan '{planId}: {planName}' for user '{userName}'.", plan.Id, plan.Name, userName);
            }
        }

        await SaveCheckpoint(checkpoint, cancellationToken);
    }

    /// <summary>
    /// Rebuilds a plan's holding tickers (market price, dividend info, and shares/avg-price from any linked
    /// portfolio) and persists it. Mirrors the server-side update logic in
    /// <c>PortfolioController.UpdateMyPortfolioPlan</c>.
    /// </summary>
    private async Task RefreshPortfolioPlanHoldings(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, PortfolioPlanEntity plan, CancellationToken cancellationToken)
    {
        var existingHoldings = plan.Metadata?.HoldingTickers ?? [];
        if (existingHoldings.Count == 0)
        {
            return;
        }

        // build a map of currently-held assets in the linked portfolio (if any), for shares/avg-price
        var assetsMap = new Dictionary<string, AssetEntity>();
        if (!string.IsNullOrWhiteSpace(plan.PortfolioId))
        {
            var assets = await portfolioRepo.GetAssetsByPortfolioIdAsync(plan.PortfolioId, cancellationToken);
            foreach (var asset in assets)
            {
                if (Globals.MarketsMap.TryGetValue(asset.MarketId?.ToUpper() ?? string.Empty, out var market))
                {
                    assetsMap[SymbolUtils.NormalizeSymbol(asset.ItemCode, market)] = asset;
                }
            }
        }

        var holdings = new List<HoldingTicker>();
        foreach (var ticker in existingHoldings)
        {
            var tickerInfoResp = await finHubClient.GetStockSymbolInfoAsync(ticker.Ticker, cancellationToken: cancellationToken);
            if (tickerInfoResp.Status != 200 || tickerInfoResp.Data == null)
            {
                Logger.LogWarning("Cannot fetch info for ticker '{ticker}' while updating plan '{planId}'; keeping existing values.", ticker.Ticker, plan.Id);
                holdings.Add(ticker);  // keep the existing holding as-is
                continue;
            }
            var data = tickerInfoResp.Data;
            var ht = new HoldingTicker
            {
                Id = ticker.Id,
                Ticker = data.NormalizedSymbol,
                TargetAllocation = ticker.TargetAllocation,
                Tags = ticker.Tags?.Trim() ?? string.Empty,
                Shares = assetsMap.TryGetValue(data.NormalizedSymbol, out var asset) ? asset.Quantity : 0,
                AveragePrice = assetsMap.TryGetValue(data.NormalizedSymbol, out var asset2) ? asset2.AveragePrice : 0,
                MarketPrice = data.StockQuote?.MarketPrice ?? 0,
                DividendYield = data.Dividend?.DividendYield ?? 0,
                PayoutFrequency = data.Dividend?.PayoutFrequency ?? 0,
            };
            var country = data.Country.ToUpper();
            if (country == "VN" || country == "VIETNAM")
            {
                // special case
                ht.MarketPrice /= 1000;
            }
            holdings.Add(ht);
        }

        plan.Metadata ??= new PortfolioPlanMetadata();
        plan.Metadata.HoldingsRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        plan.Metadata.HoldingTickers = holdings;
        var dbresult = await portfolioRepo.UpdatePortfolioPlanAsync(plan, cancellationToken);
        if (dbresult == null)
        {
            Logger.LogError("Failed to persist auto-updated portfolio plan '{planId}'.", plan.Id);
        }
    }
}
