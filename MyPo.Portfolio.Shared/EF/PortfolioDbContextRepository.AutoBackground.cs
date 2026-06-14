using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<CheckpointEntity> CheckpointStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<CheckpointEntity?> CreateCheckpointAsync(CheckpointEntity checkoint, CancellationToken cancellationToken = default)
    {
        var entry = await CheckpointStore.AddAsync(checkoint, cancellationToken);
        return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
    }

    /// <inheritdoc />
    public async ValueTask<CheckpointEntity?> GetCheckpointAsync(string ownerId, string portfolioId, string marketId, string itemCode, string checkpointType, CancellationToken cancellationToken = default)
    {
        return await CheckpointStore.AsNoTracking().FirstOrDefaultAsync(x =>
            x.OwnerId == ownerId &&
            x.PortfolioId == portfolioId &&
            x.MarketId == marketId &&
            x.ItemCode == itemCode &&
            x.CheckpointType == checkpointType, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<CheckpointEntity?> UpdateCheckpointAsync(CheckpointEntity checkoint, CancellationToken cancellationToken = default)
    {
        var existingEntry = await CheckpointStore.FindAsync([checkoint.Id], cancellationToken);
        if (existingEntry == null)
        {
            return null;
        }
        Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(checkoint));
        return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
    }

    /*----------------------------------------------------------------------*/

    private DbSet<MarketEventEntity> MarketEventStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MarketEventEntity>> GetMarketEventsAsync(string ownerId, DateTimeOffset fromDateInc, DateTimeOffset toDateExc, IEnumerable<string>? eventTypes = null, CancellationToken cancellationToken = default)
    {
        var from = fromDateInc.ToUniversalTime();
        var to = toDateExc.ToUniversalTime();
        var evTypes = (eventTypes == null || !eventTypes.Any() ? MarketEventEntity.ALL_EVENTS : eventTypes).Select(x => x.ToUpper()).ToHashSet();
        return await MarketEventStore.AsNoTracking()
            .Where(x => x.OwnerId == ownerId && x.EventTime >= from && x.EventTime < to && evTypes.Contains(x.EventType))
            .OrderBy(x => x.EventTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<MarketEventEntity?> UpsertMarketEventAsync(MarketEventEntity marketEvent, CancellationToken cancellationToken = default)
    {
        using (var tx = await Database.BeginTransactionAsync(cancellationToken))
            try
            {
                var existingEntry = await MarketEventStore.Where(
                    x => x.OwnerId == marketEvent.OwnerId
                    && x.MarketId == marketEvent.MarketId
                    && x.ItemCode == marketEvent.ItemCode
                    && x.EventType == marketEvent.EventType
                ).FirstOrDefaultAsync(cancellationToken);
                if (existingEntry == null)
                {
                    var entry = await MarketEventStore.AddAsync(marketEvent, cancellationToken);
                    existingEntry = await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
                }
                else
                {
                    marketEvent.Id = existingEntry.Id; // make sure the key is not modified
                    Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(marketEvent));
                    existingEntry = await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
                }
                await tx.CommitAsync(cancellationToken);
                return existingEntry;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteMarketEventAsync(MarketEventEntity marketEvent, CancellationToken cancellationToken = default)
    {
        MarketEventStore.Remove(marketEvent);
        return await SaveChangesAsync(cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async ValueTask<int> DeleteMarketEventsOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default)
    {
        var cutoff = cutoffDate.ToUniversalTime();
        return await MarketEventStore.Where(x => x.EventTime < cutoff).ExecuteDeleteAsync(cancellationToken);
    }
}
