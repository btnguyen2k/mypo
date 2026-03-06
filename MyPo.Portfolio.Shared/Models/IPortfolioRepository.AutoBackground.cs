namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Creates a new <see cref="CheckpointEntity" /> record.
	/// </summary>
	public ValueTask<CheckpointEntity?> CreateCheckpointAsync(CheckpointEntity checkoint, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a <see cref="CheckpointEntiry" /> by the specified parameters.
	/// </summary>
	public ValueTask<CheckpointEntity?> GetCheckpointAsync(string ownerId, string portfolioId, string marketId, string itemCode, string checkpointType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing <see cref="PortfolioEntity" /> record.
	/// </summary>
	public ValueTask<CheckpointEntity?> UpdateCheckpointAsync(CheckpointEntity checkoint, CancellationToken cancellationToken = default);

	// /// <summary>
	// /// Gets a list of <see cref="MarketEventEntity" /> records that are upcoming for the specified owner.
	// /// </summary>
	// public ValueTask<IEnumerable<MarketEventEntity>> GetUpcomingMarketEventsAsync(string ownerId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a list of <see cref="MarketEventEntity" /> records that are within the specified date range for the specified owner.
	/// </summary>
	public ValueTask<IEnumerable<MarketEventEntity>> GetMarketEventsAsync(string ownerId, DateTimeOffset fromDateInc, DateTimeOffset toDateExc, IEnumerable<string>? eventTypes = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Upserts a <see cref="CheckpointEntity" /> record.
	/// </summary>
	public ValueTask<MarketEventEntity?> UpsertMarketEventAsync(MarketEventEntity marketEvent, CancellationToken cancellationToken = default);
}
