using Microsoft.EntityFrameworkCore.Internal;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that spins up automation background tasks.
/// </summary>
[Bootstrapper]
public class AutoBackgroundBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		appBuilder.Services.AddHostedService<AutoBackgroundFindNextEarningsAnnoucements>();
		appBuilder.Services.AddHostedService<AutoBackgroundFindNextDividendAnnoucements>();
	}
}

abstract class AutoBackgroundFindNextAnnoucements : BackgroundService
{
	protected readonly IServiceProvider ServiceProvider;
	protected readonly ILogger<AutoBackgroundFindNextAnnoucements> Logger;

	protected AutoBackgroundFindNextAnnoucements(IServiceProvider serviceProvider, ILogger<AutoBackgroundFindNextAnnoucements> logger) : base()
	{
		this.ServiceProvider = serviceProvider;
		this.Logger = logger;
	}

	protected async Task<CheckpointEntity?> GetCheckpoint(
		string ownerId,
		string portfolioId,
		string marketId,
		string itemCode,
		string checkpointType,
		CancellationToken cancellationToken = default)
	{
		using var scope = ServiceProvider.CreateScope();
		var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
		var checkpoint = await portfolioRepo.GetCheckpointAsync(
			ownerId: ownerId,
			portfolioId: portfolioId,
			marketId: marketId,
			itemCode: itemCode,
			checkpointType: checkpointType,
			cancellationToken
		);
		if (checkpoint == null)
		{
			checkpoint = new()
			{
				OwnerId = ownerId.Trim().ToLower(),
				PortfolioId = portfolioId.Trim().ToLower(),
				MarketId = marketId.Trim().ToUpper(),
				ItemCode = itemCode.Trim().ToUpper(),
				CheckpointType = checkpointType.Trim().ToUpper(),
				CheckpointTime = DateTimeOffset.MinValue,
			};
			var dbresult = await portfolioRepo.CreateCheckpointAsync(checkpoint, cancellationToken);
			if (dbresult == null)
			{
				Logger.LogError("Failed to create checkpoint for market {market}.", marketId);
				checkpoint = null;
			}
		}
		return checkpoint;
	}

	protected async Task SaveEvents(IEnumerable<IncomingEarningsEvent> events, string ownerId, string marketId, CancellationToken cancellationToken = default)
	{
		using var scope = ServiceProvider.CreateScope();
		var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
		foreach (var e in events)
		{
			var marketEvent = new MarketEventEntity
			{
				OwnerId = ownerId.Trim().ToLower(),
				MarketId = marketId.Trim().ToUpper(),
				ItemCode = e.Symbol?.Trim().ToUpper()??CheckpointEntity.NON_ITEM,
				EventType = MarketEventEntity.EVENT_EARNINGS.Trim().ToUpper(),
				EventTime = e.Date,
				Metadata = new()
				{
					CompanyName = e.CompanyName,
					SourceName = e.SourceName,
					Link = e.Link,
					Status = e.Status.Trim().ToLower(),
					ReportPeriod = e.ReportPeriod.Trim().ToLower(),
				},
			};
			var dbresult = await portfolioRepo.UpsertMarketEventAsync(marketEvent, cancellationToken);
			if (dbresult == null)
			{
				Logger.LogError("Failed to upsert market event for symbol {symbol} in market {market}.", e.Symbol, marketId);
			}
		}
	}

	protected async Task SaveEvents(IEnumerable<IncomingDividendEvent> events, string ownerId, string marketId, CancellationToken cancellationToken = default)
	{
		using var scope = ServiceProvider.CreateScope();
		var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
		foreach (var e in events)
		{
			var marketEvent = new MarketEventEntity
			{
				OwnerId = ownerId,
				MarketId = marketId,
				ItemCode = e.Symbol?.ToUpper()??CheckpointEntity.NON_ITEM,
				EventType = e.EventCategory.Equals("DIVIDEND", StringComparison.OrdinalIgnoreCase) ? MarketEventEntity.EVENT_DIVIDEND : MarketEventEntity.EVENT_DISTRIBUTION,
				EventTime = e.Date,
				Metadata = new()
				{
					CompanyName = e.CompanyName,
					SourceName = e.SourceName,
					Link = e.Link,
					Status = e.Status,
					Amount = e.Amount,
					Currency = e.Currency,
				},
			};
			var dbresult = await portfolioRepo.UpsertMarketEventAsync(marketEvent, cancellationToken);
			if (dbresult == null)
			{
				Logger.LogError("Failed to upsert market event for symbol {symbol} in market {market}.", e.Symbol, marketId);
			}
		}
	}
}
