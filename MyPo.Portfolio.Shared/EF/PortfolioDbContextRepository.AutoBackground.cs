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
	public async ValueTask<IEnumerable<MarketEventEntity>> GetIncomingMarketEventsAsync(string ownerId, CancellationToken cancellationToken = default)
	{
		var now = DateTimeOffset.UtcNow;
		var currentDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
		return await MarketEventStore.AsNoTracking()
			.Where(x => x.OwnerId == ownerId && x.EventTime >= currentDate)
			.OrderBy(x => x.EventTime)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<MarketEventEntity?> UpsertMarketEventAsync(MarketEventEntity marketEvent, CancellationToken cancellationToken = default)
	{
		using (var tx = await Database.BeginTransactionAsync(cancellationToken))
		try
		{
			var existingEntry = await MarketEventStore.FindAsync([marketEvent.Id], cancellationToken);
			if (existingEntry == null)
			{
				var entry = await MarketEventStore.AddAsync(marketEvent, cancellationToken);
				existingEntry = await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
			}
			else
			{
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
}
