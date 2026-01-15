namespace MyPo.Portfolio.Shared.Models;

public interface IPortfolioRepository
{
	/// <summary>
	/// Gets portfolio records owned by a user.
	/// </summary>
	public ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserIdAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new portfolio record.
	/// </summary>
	public ValueTask<PortfolioRec?> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing portfolio record.
	/// </summary>
	public ValueTask<PortfolioRec?> UpdatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing portfolio record.
	/// </summary>
	public ValueTask<bool> DeletePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);

	/*----------------------------------------------------------------------*/

	/// <summary>
	/// Gets transaction records by portfolio ID.
	/// </summary>
	public ValueTask<IEnumerable<TransactionRec>> GetTransactionsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a transaction record by its ID.
	/// </summary>
	public ValueTask<TransactionRec?> GetTxAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new transaction record.
	/// </summary>
	public ValueTask<TransactionRec?> CreateTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing transaction record.
	/// </summary>
	public ValueTask<TransactionRec?> UpdateTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing transaction record.
	/// </summary>
	public ValueTask<bool> DeleteTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Settles a transaction record.
	/// </summary>
	public ValueTask<TransactionRec?> SettleTxAsync(TransactionRec txRec, MarketDef? market, CancellationToken cancellationToken = default);

	/*----------------------------------------------------------------------*/

	/// <summary>
	/// Gets assets by portfolio ID.
	/// </summary>
	public ValueTask<IEnumerable<Asset>> GetAssetsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);
}
