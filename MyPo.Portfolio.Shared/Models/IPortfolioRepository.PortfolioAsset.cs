namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Gets <see cref="AssetEntity" /> records by portfolio ID.
	/// </summary>
	public ValueTask<IEnumerable<AssetEntity>> GetAssetsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an <see cref="AssetEntity" /> by its ID.
	/// </summary>
	public ValueTask<AssetEntity?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Upserts an <see cref="AssetEntity" /> record.
	/// </summary>
	public ValueTask<AssetEntity?> UpdateAssetAsync(AssetEntity asset, CancellationToken cancellationToken = default);
}
