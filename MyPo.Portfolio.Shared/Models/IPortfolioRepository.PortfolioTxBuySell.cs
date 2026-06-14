namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
    /// <summary>
    /// Gets <see cref="TxBuySellEntity" /> records by portfolio ID.
    /// </summary>
    public ValueTask<IEnumerable<TxBuySellEntity>> GetTxBuySellListByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a <see cref="TxBuySellEntity" /> record by its ID.
    /// </summary>
    public ValueTask<TxBuySellEntity?> GetTxBuySellAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="TxBuySellEntity" /> record.
    /// </summary>
    public ValueTask<TxBuySellEntity?> CreateTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing <see cref="TxBuySellEntity" /> record.
    /// </summary>
    public ValueTask<TxBuySellEntity?> UpdateTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing <see cref="TxBuySellEntity" /> record.
    /// </summary>
    public ValueTask<bool> DeleteTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Settles a <see cref="TxBuySellEntity" /> record.
    /// </summary>
    public ValueTask<TxBuySellEntity?> SettleTxBuySellAsync(TxBuySellEntity tx, MarketDef? market, CancellationToken cancellationToken = default);
}
