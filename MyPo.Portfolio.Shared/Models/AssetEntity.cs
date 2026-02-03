using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class AssetEntity : Entity<string>
{
	public const string ASSET_TYPE_STOCK = "STOCK";

	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();
	public string PortfolioId { get; set; } = default!;
	public string ItemType { get; set; } = default!;
	public string ItemCode { get; set; } = default!;
	public string? MarketId { get; set; }
	public decimal Quantity { get; set; } = 0.0m;
	public decimal AveragePrice { get; set; } = 0.0m;
	public string? Tags { get; set; }
}
