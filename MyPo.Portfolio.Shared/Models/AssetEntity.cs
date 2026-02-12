using System.Text.Json.Serialization;
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
	public AssetMetadata? Metadata { get; set; }
}

public sealed class AssetMetadata
{
	[JsonPropertyName("corp_name")]
	public string CorpName { get; set; } = default!;

	[JsonPropertyName("industry")]
	public string Industry { get; set; } = default!;

	[JsonPropertyName("sector")]
	public string Sector { get; set; } = default!;

	[JsonPropertyName("tags"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ISet<string>? Tags { get; set; }
}
