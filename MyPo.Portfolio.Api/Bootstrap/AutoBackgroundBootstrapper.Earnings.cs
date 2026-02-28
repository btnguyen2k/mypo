using System.Text.Json;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class AutoBackgroundFindNextEarningsAnnoucements : AutoBackgroundFindNextAnnoucements
{
	public AutoBackgroundFindNextEarningsAnnoucements(
			IServiceProvider serviceProvider, ILogger<AutoBackgroundFindNextEarningsAnnoucements> logger
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
		await Task.Delay(Random.Shared.Next(1000, 5000), cancellationToken);

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
					checkpointType: CheckpointEntity.CHECKPOINT_INCOMING_EARNINGS,
					cancellationToken
				);

				if (checkpoint != null && (checkpoint.CheckpointTime == DateTimeOffset.MinValue || DateTimeOffset.UtcNow-checkpoint.CheckpointTime >= INTERVAL))
				{
					Logger.LogInformation("Finding next earnings announcements for market {market}...", country);
					var finhubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
					var incomingEvents = await finhubClient.GetIncomingEarningsAnnouncementsAsync(country, cancellationToken: cancellationToken);
					if (incomingEvents.Status != 200)
					{
						Logger.LogError("Failed to fetch incoming earnings announcements for market {market}. Status: {status}, Message: {message}", country, incomingEvents.Status, incomingEvents.Message);
					}
					else
					{
						Logger.LogInformation("Incoming earnings announcements for market {market}: {event}", country, JsonSerializer.Serialize(incomingEvents.Data));
						await SaveEvents(incomingEvents.Data??[], CheckpointEntity.NON_OWNER, country, cancellationToken);

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
