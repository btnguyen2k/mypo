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

	/// <inheritdoc />
	public async ValueTask<TransactionRec?> SettleTxAsync(TransactionRec tx, CancellationToken cancellationToken = default)
	{
		using var transaction = await Database.BeginTransactionAsync(cancellationToken);
		try
		{
			var existingTx = await TxRecStore.FindAsync([tx.Id], cancellationToken);
			if (existingTx == null)
			{
				await transaction.RollbackAsync(cancellationToken);
				return null;
			}

			// update owning asset
			var existingAsset = await GetAssetByOwningAsync(existingTx.PortfolioId, existingTx.ItemType, existingTx.ItemCode, existingTx.MarketId);
			if (existingAsset == null)
			{
				existingAsset = await CreateAssetAsync(new()
				{
					PortfolioId = existingTx.PortfolioId,
					ItemType = existingTx.ItemType,
					ItemCode = existingTx.ItemCode,
					MarketId = existingTx.MarketId,
					Quantity = 0.0m,
					AveragePrice = 0.0m,
				}, cancellationToken);
				if (existingAsset == null)
				{
					await transaction.RollbackAsync(cancellationToken);
					return null;
				}
			}
			var newQuantity = existingAsset.Quantity + tx.Quantity;
			var newTotalCost = existingAsset.AveragePrice * existingAsset.Quantity + tx.Price * tx.Quantity + tx.TotalFee;
			var newAveragePrice = newQuantity != 0.0m ? newTotalCost / newQuantity : 0.0m;
			existingAsset.Quantity = newQuantity;
			existingAsset.AveragePrice = newAveragePrice;
			var updatedAsset = await UpdateAssetAsync(existingAsset, cancellationToken);
			if (updatedAsset == null)
			{
				await transaction.RollbackAsync(cancellationToken);
				return null;
			}

			// update transaction record
			tx.IsSettled = true;
			var updatedTx = await UpdateTxAsync(tx, cancellationToken);
			if (updatedTx == null)
			{
				await transaction.RollbackAsync(cancellationToken);
				return null;
			}

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
}
