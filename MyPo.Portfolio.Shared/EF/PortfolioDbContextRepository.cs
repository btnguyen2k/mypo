using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyPo.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MyPo.Portfolio.Shared.EF;

public sealed class PortfolioDbContextRepository : DbContext, IPortfolioRepository
{
	private readonly ICacheFacade<IPortfolioRepository>? cache;
	private ILogger<PortfolioDbContextRepository>? logger;

	public PortfolioDbContextRepository(
		DbContextOptions<PortfolioDbContextRepository> options,
		ICacheFacade<IPortfolioRepository>? cache = default,
		ILogger<PortfolioDbContextRepository>? logger = default
		)
		: base(options)
	{
		this.cache = cache;
		this.logger = logger;
	}

	//protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	//{
	//	base.OnConfiguring(optionsBuilder);
	//}

	private void ChangeTracker_DetectedAllChanges(object? sender, DetectedChangesEventArgs e) => throw new NotImplementedException();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		new PortfolioRecEntityTypeConfiguration().Configure(modelBuilder.Entity<PortfolioRec>());
		new TransactionRecEntityTypeConfiguration().Configure(modelBuilder.Entity<TransactionRec>());
		new AssetEntityTypeConfiguration().Configure(modelBuilder.Entity<Asset>());
		new RoiRecEntityTypeConfiguration().Configure(modelBuilder.Entity<RoiRec>());
	}

	private static T PrepareForUpdate<T>(T t) where T : Entity<string>
	{
		t.UpdatedAt = DateTime.UtcNow;
		t.ConcurrencyStamp = Guid.NewGuid().ToString();
		return t;
	}

	/*----------------------------------------------------------------------*/

	private DbSet<PortfolioRec> PortfolioRecStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserIdAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await PortfolioRecStore.AsNoTracking()
			.Where(pr => pr.OwnerUserId == userId)
			.OrderBy(pr => pr.Name)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		var entry = await PortfolioRecStore.AddAsync(portfolioRec, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> GetPortfolioByIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await PortfolioRecStore.AsNoTracking().FirstOrDefaultAsync(pr => pr.Id == portfolioId, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> UpdatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		var existingEntry = await PortfolioRecStore.FindAsync([portfolioRec.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(portfolioRec));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/// <inheritdoc />
	public async ValueTask<bool> DeletePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		PortfolioRecStore.Remove(portfolioRec);
		return await SaveChangesAsync(cancellationToken) > 0;
	}

	/*----------------------------------------------------------------------*/

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
		var _ = await UpdateAssetAsync(existingAsset, cancellationToken)
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
				? $"Sold {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price} {market?.CurrencySymbol??"-"}"
				: $"Bought {tx.Quantity} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price} {market?.CurrencySymbol??"-"}",
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
				TxDesc = $"Transaction Tax for {txType} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price} {market?.CurrencySymbol??"-"}",
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
				TxDesc = $"Transaction Fee for {txType} of {tx.ItemType}/{tx.ItemCode}/{market.Code}-{market.Country} @ {tx.Price} {market?.CurrencySymbol??"-"}",
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
			await transaction.RollbackAsync(cancellationToken);
			throw e;
		}
	}

	/*----------------------------------------------------------------------*/

	private DbSet<Asset> AssetStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<Asset>> GetAssetsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await AssetStore.AsNoTracking()
			.Where(tr => tr.PortfolioId == portfolioId)
			.OrderBy(tr => tr.ItemType).OrderBy(tr => tr.ItemCode).OrderBy(tr => tr.MarketId)
			.ToListAsync(cancellationToken);
	}

	private async ValueTask<Asset?> GetAssetByOwningAsync(string portfolioId, string itemType, string itemCode, string? marketId)
	{
		return await AssetStore.AsNoTracking()
			.FirstOrDefaultAsync(a => a.PortfolioId == portfolioId
				&& a.ItemType == itemType
				&& a.ItemCode == itemCode
				&& a.MarketId == marketId);
	}

	private async ValueTask<Asset?> CreateAssetAsync(Asset asset, CancellationToken cancellationToken = default)
	{
		var entry = await AssetStore.AddAsync(asset, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<Asset?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
	{
		return await AssetStore.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default)
	{
		var existingEntry = await AssetStore.FindAsync([asset.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(asset));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/*----------------------------------------------------------------------*/

	private DbSet<RoiRec> RoiRecStore { get; set; }

	private async ValueTask<RoiRec?> CreateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default)
	{
		var entry = await RoiRecStore.AddAsync(roiRec, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<PnlSummary> GetRoiSummaryForPortfolioAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		var roiSummary = new PnlSummary()
		{
			PortfolioId = portfolioId,
			TotalBuyValue = 0.0m,
			TotalSellValue = 0.0m,
			TotalDividends = 0.0m,
			TotalFees = 0.0m,
			TotalCashIn = 0.0m,
			TotalCashOut = 0.0m,
		};
		var rows = await RoiRecStore.AsNoTracking()
			.Where(rr => rr.PortfolioId == portfolioId)
			.Where(rr => rr.Status != RoiRec.STATUS_ARCHIVED)
			.GroupBy(rr => rr.TxType)
			.Select(g => new
			{
				TxType = g.Key,
				TotalValue = g.Sum(rr => rr.TxValue)
			})
			.ToListAsync(cancellationToken);
		foreach (var row in rows)
		{
			switch (row.TxType)
			{
				case RoiRec.TX_TYPE_BUY:
					roiSummary.TotalBuyValue = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_SELL:
					roiSummary.TotalSellValue = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_DIVIDEND:
					roiSummary.TotalDividends = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_FEE:
					roiSummary.TotalFees = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_CASHIN:
					roiSummary.TotalCashIn = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_CASHOUT:
					roiSummary.TotalCashOut = row.TotalValue;
					break;
			}
		}

		return roiSummary;
	}

	/// <inheritdoc />
	public async ValueTask<IEnumerable<RoiRec>> GetRoiRecsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await RoiRecStore.AsNoTracking()
			.Where(rr => rr.PortfolioId == portfolioId).Where(rr => rr.Status != RoiRec.STATUS_ARCHIVED)
			.OrderByDescending(rr => rr.TxTime)
			.ToListAsync(cancellationToken);
	}
}
