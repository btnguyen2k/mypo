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
        appBuilder.Services.AddHostedService<AutoBackgroundOldEventsCleaner>();

        // appBuilder.Services.AddHostedService<AutoBackgroundUpcomingDividendAnnouncementsScanner>();
        // appBuilder.Services.AddHostedService<AutoBackgroundUpcomingEarningsAnnouncementsScanner>();
        // appBuilder.Services.AddHostedService<AutoBackgroundNewListingAnnouncementsScanner>();

        // appBuilder.Services.AddHostedService<AutoBackgroundUpdatePortfolioPlansScanner>();
        // appBuilder.Services.AddHostedService<AutoBackgroundAnalyzePortfolioPlansScanner>();

        // appBuilder.Services.AddHostedService<AutoBackgroundSendMarketAlerts>();

        appBuilder.Services.AddHostedService<AutoBackgroundReporting>();
    }
}

abstract class AutoBackgroundAnnouncementScanner : BackgroundService
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger<AutoBackgroundAnnouncementScanner> Logger;

    protected AutoBackgroundAnnouncementScanner(IServiceProvider serviceProvider, ILogger<AutoBackgroundAnnouncementScanner> logger) : base()
    {
        this.ServiceProvider = serviceProvider;
        this.Logger = logger;
    }

    protected async Task<CheckpointEntity?> GetOrInitCheckpoint(
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
                Logger.LogError(
                    "Failed to create checkpoint: Owner: {owner} - Portfolio: {portfolio} - Market: {market} - Item: {item} - Type: {type}.",
                    checkpoint.OwnerId, checkpoint.PortfolioId, checkpoint.MarketId, checkpoint.ItemCode, checkpoint.CheckpointType
                );
                checkpoint = null;
            }
        }
        return checkpoint;
    }

    protected async Task SaveCheckpoint(CheckpointEntity checkpoint, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
        var dbresult = await portfolioRepo.UpdateCheckpointAsync(checkpoint, cancellationToken);
        if (dbresult == null)
        {
            Logger.LogError(
                "Failed to save checkpoint: Owner: {owner} - Portfolio: {portfolio} - Market: {market} - Item: {item} - Type: {type}.",
                checkpoint.OwnerId, checkpoint.PortfolioId, checkpoint.MarketId, checkpoint.ItemCode, checkpoint.CheckpointType
            );
        }
    }

    protected async Task SaveEvents(IEnumerable<UpcomingDividendEvent> events, string ownerId, string marketId, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        foreach (var e in events)
        {
            var marketEvent = new MarketEventEntity
            {
                OwnerId = ownerId,
                MarketId = marketId,
                ItemCode = e.Symbol?.ToUpper() ?? CheckpointEntity.NON_ITEM,
                EventType = MarketEventEntity.EVENT_DISTRIBUTION.Equals(e.EventCategory, StringComparison.OrdinalIgnoreCase) ? MarketEventEntity.EVENT_DISTRIBUTION : MarketEventEntity.EVENT_DIVIDEND,
                EventTime = e.Date,
                Metadata = new()
                {
                    Exchange = e.Exchange,
                    CompanyName = e.CompanyName,
                    SourceName = e.SourceName,
                    Link = e.Link,
                    Status = e.Status,
                    Currency = e.Currency,
                    // Capital = e.Analysis?.Overview?.MarketCap ?? 0,
                    Dividend = new()
                    {
                        PaymentDate = e.PaymentDate,
                        Amount = e.Amount,
                        DividendYield = e.DividendYield,
                        Analysis = e.Analysis,
                    },
                },
            };
            var dbresult = await portfolioRepo.UpsertMarketEventAsync(marketEvent, cancellationToken);
            if (dbresult == null)
            {
                Logger.LogError("Failed to upsert market event for symbol {symbol} in market {market}.", e.Symbol, marketId);
            }
        }
    }

    protected async Task SaveEvents(IEnumerable<UpcomingEarningsEvent> events, string ownerId, string marketId, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        foreach (var e in events)
        {
            var marketEvent = new MarketEventEntity
            {
                OwnerId = ownerId.Trim().ToLower(),
                MarketId = marketId.Trim().ToUpper(),
                ItemCode = e.Symbol?.Trim().ToUpper() ?? CheckpointEntity.NON_ITEM,
                EventType = MarketEventEntity.EVENT_EARNINGS,
                EventTime = e.Date,
                Metadata = new()
                {
                    Exchange = e.Exchange,
                    CompanyName = e.CompanyName,
                    SourceName = e.SourceName,
                    Link = e.Link,
                    Status = e.Status?.Trim().ToLower() ?? "n/a",
                    Earnings = new()
                    {
                        ReportPeriod = e.ReportPeriod?.Trim().ToLower() ?? "n/a",
                    },
                },
            };
            var dbresult = await portfolioRepo.UpsertMarketEventAsync(marketEvent, cancellationToken);
            if (dbresult == null)
            {
                Logger.LogError("Failed to upsert market event for symbol {symbol} in market {market}.", e.Symbol, marketId);
            }
        }
    }

    protected async Task SaveEvents(IEnumerable<ListingEvent> events, string ownerId, string marketId, CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        foreach (var e in events)
        {
            var marketEvent = new MarketEventEntity
            {
                OwnerId = ownerId.Trim().ToLower(),
                MarketId = marketId.Trim().ToUpper(),
                ItemCode = e.Symbol?.Trim().ToUpper() ?? CheckpointEntity.NON_ITEM,
                EventType = MarketEventEntity.EVENT_LISTING,
                EventTime = e.Date,
                Metadata = new()
                {
                    Exchange = e.Exchange,
                    CompanyName = e.CompanyName,
                    SourceName = e.SourceName,
                    Link = e.Link,
                    Sector = e.Sector,
                    Industry = e.Industry,
                    Currency = e.Currency,
                    Capital = e.Capital,
                    Listing = new()
                    {
                        Price = e.Price,
                        Analysis = e.Analysis,
                    },
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
