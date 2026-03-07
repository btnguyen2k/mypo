using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class AutoBackgroundUpcomingDividendAnnouncementsScanner : AutoBackgroundAnnouncementScanner
{
	public AutoBackgroundUpcomingDividendAnnouncementsScanner(
			IServiceProvider serviceProvider, ILogger<AutoBackgroundUpcomingDividendAnnouncementsScanner> logger
		) : base(serviceProvider, logger)
	{
	}

	// TODO: move this to configuration
	private static readonly List<string> COUNTRIES = ["AU", "VN", "US"];
	private static readonly TimeSpan INTERVAL = TimeSpan.FromHours(18);

	private int currentCountryIndex = Random.Shared.Next(0, COUNTRIES.Count);

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// delay a bit to avoid all instances running at the same time after deployment or restart
		await Task.Delay(Random.Shared.Next(10000, 30000), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
			using (var scope = ServiceProvider.CreateScope())
			try
			{
				var country = COUNTRIES[currentCountryIndex++ % COUNTRIES.Count].Trim().ToUpper();
				var checkpoint = await GetCheckpoint(
					ownerId: CheckpointEntity.NON_OWNER,
					portfolioId: CheckpointEntity.NON_PORTFOLIO,
					marketId: country,
					itemCode: CheckpointEntity.NON_ITEM,
					checkpointType: CheckpointEntity.CHECKPOINT_UPCOMING_DIVIDEND,
					cancellationToken
				);

				if (checkpoint != null && (checkpoint.CheckpointTime == DateTimeOffset.MinValue || DateTimeOffset.UtcNow-checkpoint.CheckpointTime >= INTERVAL))
				{
					Logger.LogInformation("Finding upcoming dividend/distribution announcements for market {market}...", country);
					var finhubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
					var events = await finhubClient.GetUpcomingDividendAnnouncementsAsync(country, cancellationToken: cancellationToken);
					if (events.Status != 200)
					{
						Logger.LogError("Failed to fetch upcoming dividend/distribution announcements for market {market}. Status: {status}, Message: {message}", country, events.Status, events.Message);
					}
					else
					{
						Logger.LogInformation("Upcoming dividend/distribution announcements for market {market}: {event}", country, (events.Data??[]).Count());
						await SaveEvents(events.Data??[], CheckpointEntity.NON_OWNER, country, cancellationToken);

						checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
						var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
						var dbresult = await portfolioRepo.UpdateCheckpointAsync(checkpoint, cancellationToken);
						if (dbresult == null)
						{
							Logger.LogError("Failed to update checkpoint for market {market}.", country);
						}
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
