using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
	private DbSet<TxBuySellEntity> TxBuySellStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<TxBuySellEntity>> GetTxBuySellListByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await TxBuySellStore.AsNoTracking()
			.Where(tr => tr.PortfolioId == portfolioId)
			.OrderByDescending(tr => tr.Time)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<TxBuySellEntity?> GetTxBuySellAsync(string id, CancellationToken cancellationToken = default)
	{
		return await TxBuySellStore.AsNoTracking().FirstOrDefaultAsync(tr => tr.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<TxBuySellEntity?> CreateTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default)
	{
		var entry = await TxBuySellStore.AddAsync(tx, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<TxBuySellEntity?> UpdateTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default)
	{
		var existingEntry = await TxBuySellStore.FindAsync([tx.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(tx));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/// <inheritdoc />
	public async ValueTask<bool> DeleteTxBuySellAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default)
	{
		TxBuySellStore.Remove(tx);
		return await SaveChangesAsync(cancellationToken) > 0;
	}

	/// <summary>
	/// Updates the owning asset after settling the transaction, using Average method.
	/// </summary>
	/// <param name="tx"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task SettleTxUpdateAssetAsync(TxBuySellEntity tx, CancellationToken cancellationToken = default)
	{
		// only accept transaction type of SELL or BUY
		if (!tx.Type.Equals(TxBuySellEntity.TX_TYPE_BUY, StringComparison.OrdinalIgnoreCase)
			&& !tx.Type.Equals(TxBuySellEntity.TX_TYPE_SELL, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Only buy/sell transactions can update assets.");
		}

		// get existing owning asset, create if not exist
		var existingAsset = await GetAssetByOwningAsync(tx.PortfolioId, tx.ItemType, tx.ItemCode, tx.MarketId);
		if (existingAsset == null)
		{
			logger?.LogInformation("SettleTx - (Tx: {txid}) No existing asset {portfolioId}: {itemType}/{itemCode}/{marketId} found for the transaction. Creating new asset record.",
				tx.Id.Replace(Environment.NewLine, ""),
				tx.PortfolioId.Replace(Environment.NewLine, ""),
				tx.ItemType.Replace(Environment.NewLine, ""),
				tx.ItemCode.Replace(Environment.NewLine, ""),
				tx.MarketId?.Replace(Environment.NewLine, "")??""
			);
			existingAsset = await CreateAssetAsync(new()
			{
				PortfolioId = tx.PortfolioId,
				ItemType = tx.ItemType,
				ItemCode = tx.ItemCode,
				MarketId = tx.MarketId,
				Quantity = 0.0m,
				AveragePrice = 0.0m,
			}, cancellationToken);
			if (existingAsset == null)
			{
				throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to create owning asset.");
			}
		}
		var txType = tx.Type.Trim().ToUpper();

		// validate quantity for sell transaction
		if (txType.Equals(TxBuySellEntity.TX_TYPE_SELL, StringComparison.OrdinalIgnoreCase) && existingAsset.Quantity < tx.Quantity)
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Insufficient asset quantity to settle sell transaction. Available: {existingAsset.Quantity}, Required: {tx.Quantity}.");
		}

		// update owning asset
		var newQuantity = existingAsset.Quantity + (txType.Equals(TxBuySellEntity.TX_TYPE_BUY, StringComparison.OrdinalIgnoreCase) ? tx.Quantity : -tx.Quantity);
		if (tx.Type.Equals(TxBuySellEntity.TX_TYPE_BUY, StringComparison.OrdinalIgnoreCase))
		{
			// update owning asset average price for buy transaction
			var assetTotalCost = existingAsset.AveragePrice * existingAsset.Quantity;
			var txBaseCost = tx.Price * tx.Quantity;
			var newTotalCost = assetTotalCost + txBaseCost + tx.TotalFee;
			existingAsset.AveragePrice = newQuantity != 0.0m ? newTotalCost / newQuantity : 0.0m;
		}
		existingAsset.Quantity = newQuantity;
		if (newQuantity == 0.0m)
		{
			existingAsset.AveragePrice = 0.0m; // reset average price if quantity is zero
		}
		_ = await UpdateAssetAsync(existingAsset, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to update owning asset.");
	}

	private async Task SettleTxUpdateSettlementAsync(PortfolioEntity portfolio, TxBuySellEntity txBuySell, MarketDef market, CancellationToken cancellationToken = default)
	{
		if (!portfolio.Currency.Equals(market.Currency, StringComparison.OrdinalIgnoreCase))
		{
			// ignore Settlement record if market currency is different from portfolio currency
			logger?.LogWarning("SettleTx - (Tx: {txid}) Market currency ({marketCurrency}) is different from portfolio currency ({portfolioCurrency}). Skipping Settlement record creation.",
				txBuySell.Id, market.Currency, portfolio.Currency);
			return;
		}

		var txType = txBuySell.Type.Trim().ToUpper();
		var txSettlement = new TxSettlementEntity()
		{
			PortfolioId = txBuySell.PortfolioId,
			Status = TxSettlementEntity.STATUS_FINAL,
			TxType = txType == TxBuySellEntity.TX_TYPE_SELL ? TxSettlementEntity.TX_TYPE_SELL : TxSettlementEntity.TX_TYPE_BUY,
			TxTime = txBuySell.Time,
			TxValue = txBuySell.Price * txBuySell.Quantity,
			RefTxId = txBuySell.Id,
			RefItemType = txBuySell.ItemType,
			RefItemCode = txBuySell.ItemCode,
			RefMarketId = txBuySell.MarketId,
			TxDesc = txType == TxBuySellEntity.TX_TYPE_SELL
				? $"Sold {txBuySell.Quantity} of {txBuySell.ItemType}/{txBuySell.ItemCode}/{market.Code}-{market.Country} @ {txBuySell.Price}"
				: $"Bought {txBuySell.Quantity} of {txBuySell.ItemType}/{txBuySell.ItemCode}/{market.Code}-{market.Country} @ {txBuySell.Price}",
		};
		_ = await CreateTxSettlementAsync(txSettlement, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {txBuySell.Id}) Failed to create Settlement record.");

		if (txBuySell.FeeTax != 0.0m)
		{
			var taxTxSettlement = new TxSettlementEntity()
			{
				PortfolioId = txBuySell.PortfolioId,
				Status = TxSettlementEntity.STATUS_FINAL,
				TxType = TxSettlementEntity.TX_TYPE_TAX,
				TxTime = txBuySell.Time.AddSeconds(1),
				TxValue = txBuySell.FeeTax,
				RefTxId = txBuySell.Id,
				RefItemType = txBuySell.ItemType,
				RefItemCode = txBuySell.ItemCode,
				RefMarketId = txBuySell.MarketId,
				TxDesc = $"Tax for {txType} {txBuySell.Quantity} of {txBuySell.ItemType}/{txBuySell.ItemCode}/{market!.Code}-{market.Country} @ {txBuySell.Price}",
			};
			_ = await CreateTxSettlementAsync(taxTxSettlement, cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {txBuySell.Id}) Failed to create Settlement record.");
		}

		if (txBuySell.FeeTx != 0.0m || txBuySell.FeeOther != 0.0m)
		{
			var feeTxSettlement = new TxSettlementEntity()
			{
				PortfolioId = txBuySell.PortfolioId,
				Status = TxSettlementEntity.STATUS_FINAL,
				TxType = TxSettlementEntity.TX_TYPE_FEE,
				TxTime = txBuySell.Time.AddSeconds(2),
				TxValue = txBuySell.FeeTx + txBuySell.FeeOther,
				RefTxId = txBuySell.Id,
				RefItemType = txBuySell.ItemType,
				RefItemCode = txBuySell.ItemCode,
				RefMarketId = txBuySell.MarketId,
				TxDesc = $"Fee for {txType} {txBuySell.Quantity} of {txBuySell.ItemType}/{txBuySell.ItemCode}/{market!.Code}-{market.Country} @ {txBuySell.Price}",
			};
			_ = await CreateTxSettlementAsync(feeTxSettlement, cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {txBuySell.Id}) Failed to create Settlement record.");
		}
	}

	/// <inheritdoc />
	public async ValueTask<TxBuySellEntity?> SettleTxBuySellAsync(TxBuySellEntity tx, MarketDef? market, CancellationToken cancellationToken = default)
	{
		var txType = tx.Type.Trim().ToUpper();
		if (txType != TxBuySellEntity.TX_TYPE_BUY && txType != TxBuySellEntity.TX_TYPE_SELL)
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Only buy/sell transactions can be settled.");
		}
		var portfolio = await GetPortfolioByIdAsync(tx.PortfolioId, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Portfolio {tx.PortfolioId} not found.");
		using var transaction = await Database.BeginTransactionAsync(cancellationToken);
		try
		{
			var existingTx = await TxBuySellStore.FindAsync([tx.Id], cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Transaction record not found.");
			if (existingTx.IsSettled)
			{
				throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Transaction has already been settled.");
			}
			if (!existingTx.PortfolioId.Equals(tx.PortfolioId,StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Transaction portfolio ID mismatch (Input: {tx.PortfolioId}) vs Existing: {existingTx.PortfolioId}.");
			}

			await SettleTxUpdateAssetAsync(tx, cancellationToken);

			if (market != null)
			{
				await SettleTxUpdateSettlementAsync(portfolio, tx, market, cancellationToken);
			}

			tx.IsSettled = true;
			var updatedTx = await UpdateTxBuySellAsync(tx, cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to update transaction record.");

			await transaction.CommitAsync(cancellationToken);
			return updatedTx;
		}
		catch (Exception e)
		{
			logger?.LogError(e, "SettleTx - (Tx: {txid}) Failed to settle transaction.", tx.Id);
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}
}
