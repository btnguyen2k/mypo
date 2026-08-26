using Ddth.Utilities.Tempus;
using Finhub.Client;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class BackgroundPortfolioTaskOldEventsCleaner : BackgroundPortfolioTask
{
    public BackgroundPortfolioTaskOldEventsCleaner(
            IServiceProvider serviceProvider, ILogger<BackgroundPortfolioTaskOldEventsCleaner> logger
        ) : base(serviceProvider, logger)
    {
    }

    // Run task ~one per day.
    private static readonly TimeSpan INTERVAL = TimeSpan.FromHours(24);

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
                    var checkpoint = await GetOrInitCheckpoint(
                        ownerId: CheckpointEntity.NON_OWNER,
                        portfolioId: CheckpointEntity.NON_PORTFOLIO,
                        marketId: CheckpointEntity.NON_MARKET,
                        itemCode: CheckpointEntity.NON_ITEM,
                        checkpointType: CheckpointEntity.CHECKPOINT_REMOVE_OLD_EVENTS,
                        cancellationToken
                    );

                    if (checkpoint != null && (checkpoint.CheckpointTime == DateTimeOffset.MinValue || DateTimeOffset.UtcNow - checkpoint.CheckpointTime >= INTERVAL))
                    {
                        Logger.LogInformation("Removing old events...");
                        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();

                        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
                        var deletedCount = await portfolioRepo.DeleteMarketEventsOlderThanAsync(cutoffDate, cancellationToken);
                        Logger.LogInformation("Removed {deletedCount} events older than {cutoffDate}.", deletedCount, cutoffDate);

                        var eventsListing = (await portfolioRepo.GetMarketEventsAsync(
                            MarketEventEntity.NON_OWNER,
                            DateTimeOffset.UtcNow.StartOfDay().AddDays(-14), DateTimeOffset.UtcNow.StartOfDay().AddDays(-1),
                            [MarketEventEntity.EVENT_LISTING],
                            cancellationToken: cancellationToken)).ToList();
                        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
                        var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(
                            eventsListing.Select(e => e.ItemCode).Distinct(),
                            finHubClient, cancellationToken: cancellationToken);
                        foreach (var e in eventsListing)
                        {
                            var ticker = YFUtils.BuildYFTicker(e.ItemCode);
                            if (!quotesMap.TryGetValue(ticker, out _))
                            {
                                Logger.LogInformation("Deleting invalid listing event for {itemCode}...", e.ItemCode);
                                await portfolioRepo.DeleteMarketEventAsync(e, cancellationToken: cancellationToken);
                            }
                        }

                        checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
                        var dbresult = await portfolioRepo.UpdateCheckpointAsync(checkpoint, cancellationToken);
                        if (dbresult == null)
                        {
                            Logger.LogError(
                                "Failed to update checkpoint: Owner: {owner} - Portfolio: {portfolio} - Market: {market} - Item: {item} - Type: {type}.",
                                checkpoint.OwnerId, checkpoint.PortfolioId, checkpoint.MarketId, checkpoint.ItemCode, checkpoint.CheckpointType
                            );
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
}
