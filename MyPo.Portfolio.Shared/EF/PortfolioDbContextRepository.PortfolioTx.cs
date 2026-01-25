using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
	private DbSet<TransactionRec> TxRecStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<TransactionRec>> GetTransactionsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await TxRecStore.AsNoTracking()
			.Where(tr => tr.PortfolioId == portfolioId)
			.OrderByDescending(tr => tr.Time)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<TransactionRec?> GetTxAsync(string id, CancellationToken cancellationToken = default)
	{
		return await TxRecStore.AsNoTracking().FirstOrDefaultAsync(tr => tr.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<TransactionRec?> CreateTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default)
	{
		var entry = await TxRecStore.AddAsync(txRec, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<TransactionRec?> UpdateTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default)
	{
		var existingEntry = await TxRecStore.FindAsync([txRec.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(txRec));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/// <inheritdoc />
	public async ValueTask<bool> DeleteTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default)
	{
		TxRecStore.Remove(txRec);
		return await SaveChangesAsync(cancellationToken) > 0;
	}

	/// <summary>
	/// Updates the owning asset after settling the transaction, using Average method.
	/// </summary>
	/// <param name="tx"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task SettleTxUpdateAssetAsync(TransactionRec tx, CancellationToken cancellationToken = default)
	{
		// only accept transaction type of SELL or BUY
		if (!tx.Type.Equals(TransactionRec.TXTYPE_BUY, StringComparison.OrdinalIgnoreCase)
			&& !tx.Type.Equals(TransactionRec.TXTYPE_SELL, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Only buy/sell transactions can update assets.");
		}

		// get existing owning asset, create if not exist
		var existingAsset = await GetAssetByOwningAsync(tx.PortfolioId, tx.ItemType, tx.ItemCode, tx.MarketId);
		if (existingAsset == null)
		{
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
		if (txType.Equals(TransactionRec.TXTYPE_SELL, StringComparison.OrdinalIgnoreCase) && existingAsset.Quantity < tx.Quantity)
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Insufficient asset quantity to settle sell transaction. Available: {existingAsset.Quantity}, Required: {tx.Quantity}.");
		}

		// update owning asset
		var newQuantity = existingAsset.Quantity + (txType.Equals(TransactionRec.TXTYPE_BUY, StringComparison.OrdinalIgnoreCase) ? tx.Quantity : -tx.Quantity);
		if (tx.Type.Equals(TransactionRec.TXTYPE_BUY,StringComparison.OrdinalIgnoreCase))
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

	private async Task SettleTxUpdateRoiAsync(PortfolioRec portfolio, TransactionRec tx, MarketDef market, CancellationToken cancellationToken = default)
	{
		if (!portfolio.Currency.Equals(market.Currency, StringComparison.OrdinalIgnoreCase))
		{
			// ignore ROI record if market currency is different from portfolio currency
			logger?.LogWarning("SettleTx - (Tx: {txid}) Market currency ({marketCurrency}) is different from portfolio currency ({portfolioCurrency}). Skipping ROI record creation.",
				tx.Id, market.Currency, portfolio.Currency);
			return;
		}

		var txType = tx.Type.Trim().ToUpper();
		var roiRec = new RoiRec()
		{
			PortfolioId = tx.PortfolioId,
			Status = RoiRec.STATUS_FINAL,
			TxType = txType == TransactionRec.TXTYPE_SELL ? RoiRec.TX_TYPE_SELL : RoiRec.TX_TYPE_BUY,
			TxTime = tx.Time,
			TxValue = tx.Price * tx.Quantity,
			RefTxId = tx.Id,
			RefItemType = tx.ItemType,
			RefItemCode = tx.ItemCode,
			RefMarketId = tx.MarketId,
			TxDesc = txType == TransactionRec.TXTYPE_SELL
				? $"Sold {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price}"
				: $"Bought {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price}",
		};
		_ = await CreateRoiRecAsync(roiRec, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to create ROI record.");

		if (tx.FeeTax != 0.0m)
		{
			var taxRoiRec = new RoiRec()
			{
				PortfolioId = tx.PortfolioId,
				Status = RoiRec.STATUS_FINAL,
				TxType = RoiRec.TX_TYPE_TAX,
				TxTime = tx.Time,
				TxValue = tx.FeeTax,
				RefTxId = tx.Id,
				RefItemType = tx.ItemType,
				RefItemCode = tx.ItemCode,
				RefMarketId = tx.MarketId,
				TxDesc = $"Tax for {txType} {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market!.Code}-{market.Country} @ {tx.Price}",
			};
			_ = await CreateRoiRecAsync(taxRoiRec, cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to create ROI record.");
		}

		if (tx.FeeTx != 0.0m || tx.FeeOther != 0.0m)
		{
			var feeRoiRec = new RoiRec()
			{
				PortfolioId = tx.PortfolioId,
				Status = RoiRec.STATUS_FINAL,
				TxType = RoiRec.TX_TYPE_FEE,
				TxTime = tx.Time,
				TxValue = tx.FeeTx + tx.FeeOther,
				RefTxId = tx.Id,
				RefItemType = tx.ItemType,
				RefItemCode = tx.ItemCode,
				RefMarketId = tx.MarketId,
				TxDesc = $"Fee for {txType} {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market!.Code}-{market.Country} @ {tx.Price}",
			};
			_ = await CreateRoiRecAsync(feeRoiRec, cancellationToken)
				?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to create ROI record.");
		}
	}

	/// <inheritdoc />
	public async ValueTask<TransactionRec?> SettleTxAsync(TransactionRec tx, MarketDef? market, CancellationToken cancellationToken = default)
	{
		var txType = tx.Type.Trim().ToUpper();
		if (txType != TransactionRec.TXTYPE_BUY && txType != TransactionRec.TXTYPE_SELL)
		{
			throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Only buy/sell transactions can be settled.");
		}
		var portfolio = await GetPortfolioByIdAsync(tx.PortfolioId, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Portfolio {tx.PortfolioId} not found.");
		using var transaction = await Database.BeginTransactionAsync(cancellationToken);
		try
		{
			var existingTx = await TxRecStore.FindAsync([tx.Id], cancellationToken)
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
				await SettleTxUpdateRoiAsync(portfolio, tx, market, cancellationToken);
			}

			tx.IsSettled = true;
			var updatedTx = await UpdateTxAsync(tx, cancellationToken)
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
