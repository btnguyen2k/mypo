using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class AutoBackgroundOldEventsCleaner : AutoBackgroundAnnouncementScanner
{
	public AutoBackgroundOldEventsCleaner(
			IServiceProvider serviceProvider, ILogger<AutoBackgroundOldEventsCleaner> logger
		) : base(serviceProvider, logger)
	{
	}

	// Run task ~one per day.
	private static readonly TimeSpan INTERVAL = TimeSpan.FromHours(24);

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// delay a bit to avoid all instances running at the same time after deployment or restart
		await Task.Delay(Random.Shared.Next(10000, 30000), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
			using (var scope = ServiceProvider.CreateScope())
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

				if (checkpoint != null && (checkpoint.CheckpointTime == DateTimeOffset.MinValue || DateTimeOffset.UtcNow-checkpoint.CheckpointTime >= INTERVAL))
				{
					Logger.LogInformation("Removing old events...");
					var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();

					var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
					var deletedCount = await portfolioRepo.DeleteMarketEventsOlderThanAsync(cutoffDate, cancellationToken);
					Logger.LogInformation("Removed {deletedCount} events older than {cutoffDate}.", deletedCount, cutoffDate);

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
            try
            {
				var delaySecs = Random.Shared.Next(10*60, 20*60);
				Logger.LogInformation("Waiting for {delaySecs} seconds before the next execution...", delaySecs);
                await Task.Delay(delaySecs*1000, cancellationToken);
            }
            catch (TaskCanceledException) {}
        }
	}
}
