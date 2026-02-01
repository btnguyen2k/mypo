namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Gets <see cref="PnlSummary"/> for a portfolio.
	/// </summary>
	public ValueTask<PnlSummary> GetPnlSummaryForPortfolioAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets <see cref="TxSettlementEntity" /> records for a portfolio.
	/// </summary>
	public ValueTask<IEnumerable<TxSettlementEntity>> GetTxSettlementsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new <see cref="TxSettlementEntity" /> record.
	/// </summary>
	public ValueTask<TxSettlementEntity?> CreateTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a <see cref="TxSettlementEntity" /> record by its ID.
	/// </summary>
	public ValueTask<TxSettlementEntity?> GetTxSettlementByIdAsync(string txId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing <see cref="TxSettlementEntity" /> record.
	/// </summary>
	public ValueTask<TxSettlementEntity?> UpdateTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing <see cref="TxSettlementEntity" /> record.
	/// </summary>
	public ValueTask<bool> DeleteTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default);
}
