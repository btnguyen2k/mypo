using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.EF;

public sealed class PortfolioDbContextRepository : DbContext, IPortfolioRepository
{
	private readonly ICacheFacade<IPortfolioRepository>? cache;

	public PortfolioDbContextRepository(DbContextOptions<PortfolioDbContextRepository> options, ICacheFacade<IPortfolioRepository>? cache = default)
		: base(options)
	{
		this.cache = cache;
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

	private async Task SettleTxUpdateAssetAsync(TransactionRec tx, CancellationToken cancellationToken = default)
	{
		// update owning asset
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
		var newQuantity = existingAsset.Quantity + (txType==TransactionRec.TXTYPE_BUY ? tx.Quantity : -tx.Quantity);
		var assetTotalCost = existingAsset.AveragePrice * existingAsset.Quantity;
		var txBaseCost = tx.Price * tx.Quantity;
		var newTotalCost = assetTotalCost + (txType==TransactionRec.TXTYPE_BUY ? txBaseCost : -txBaseCost) + tx.TotalFee;
		var newAveragePrice = newQuantity != 0.0m ? newTotalCost / newQuantity : 0.0m;
		existingAsset.Quantity = newQuantity;
		existingAsset.AveragePrice = newAveragePrice;
		var _ = await UpdateAssetAsync(existingAsset, cancellationToken)
			?? throw new InvalidOperationException($"SettleTx - (Tx: {tx.Id}) Failed to update owning asset.");
	}

	private async Task SettleTxUpdateRoiAsync(TransactionRec tx, MarketDef? market, CancellationToken cancellationToken = default)
	{
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
				? $"Sold {tx.Quantity} of ({tx.ItemType} - {tx.ItemCode}) @ {market?.CurrencySymbol??""} {tx.Price}"
				: $"Bought {tx.Quantity} of ({tx.ItemType} - {tx.ItemCode}) @ {market?.CurrencySymbol??""} {tx.Price}",
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
				TxDesc = $"Transaction Tax for @{txType} ({tx.ItemType} - {tx.ItemCode}) @ {market?.CurrencySymbol??""} {tx.Price}",
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
				TxDesc = $"Transaction Fee for @{txType} ({tx.ItemType} - {tx.ItemCode}) @ {market?.CurrencySymbol??""} {tx.Price}",
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

			await SettleTxUpdateRoiAsync(tx, market, cancellationToken);

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

	private async ValueTask<Asset?> UpdateAssetAsync(Asset asset, CancellationToken cancellationToken = default)
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

	private async ValueTask<RoiRec?> UpdateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default)
	{
		var existingEntry = await RoiRecStore.FindAsync([roiRec.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(roiRec));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}
}
