using Finhub.Client;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class BackgroundPortfolioTaskNewListingAnnouncementsScanner : BackgroundPortfolioTask
{
    public BackgroundPortfolioTaskNewListingAnnouncementsScanner(
            IServiceProvider serviceProvider, ILogger<BackgroundPortfolioTaskNewListingAnnouncementsScanner> logger
        ) : base(serviceProvider, logger)
    {
    }

    // TODO: move this to configuration
    private static readonly List<string> COUNTRIES = ["AU"];

    // Run task ~every 2 days per market
    private static readonly TimeSpan INTERVAL = TimeSpan.FromHours(13);

    private int currentCountryIndex = Random.Shared.Next(0, COUNTRIES.Count);

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
                    var country = COUNTRIES[currentCountryIndex++ % COUNTRIES.Count].Trim().ToUpper();
                    if (currentCountryIndex > 1000) currentCountryIndex %= COUNTRIES.Count;
                    var checkpoint = await GetOrInitCheckpoint(
                        ownerId: CheckpointEntity.NON_OWNER,
                        portfolioId: CheckpointEntity.NON_PORTFOLIO,
                        marketId: country,
                        itemCode: CheckpointEntity.NON_ITEM,
                        checkpointType: CheckpointEntity.CHECKPOINT_NEW_LISTINGS,
                        cancellationToken
                    );

                    if (checkpoint != null && (checkpoint.CheckpointTime == DateTimeOffset.MinValue || DateTimeOffset.UtcNow - checkpoint.CheckpointTime >= INTERVAL))
                    {
                        Logger.LogInformation("Finding new listing announcements for market {market}...", country);
                        var finhubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
                        var events = await finhubClient.GetNewListingAnnouncementsAsync(country, cancellationToken: cancellationToken);
                        if (events.Status != 200)
                        {
                            Logger.LogError("Failed to fetch new listing announcements for market {market}. Status: {status}, Message: {message}", country, events.Status, events.Message);
                        }
                        else
                        {
                            var eventsList = events.Data ?? [];
                            Logger.LogInformation("New listing announcements for market {market}: {event}", country, eventsList.Count());
                            foreach (var e in eventsList)
                            {
                                Logger.LogInformation("- {date} - {symbol} {name}", e.Date, e.Symbol, e.CompanyName);
                            }
                            await SaveEvents(eventsList, CheckpointEntity.NON_OWNER, country, cancellationToken);

                            checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
                            var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
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
