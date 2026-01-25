using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
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
}
