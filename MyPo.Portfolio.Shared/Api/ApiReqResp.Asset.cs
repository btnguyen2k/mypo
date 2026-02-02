using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdateAssetReq
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("item_type")]
	public string ItemType { get; set; }

	[JsonPropertyName("item_code")]
	public string ItemCode { get; set; }

	[JsonPropertyName("quantity")]
	public decimal Quantity { get; set; }

	[JsonPropertyName("average_price")]
	public decimal AveragePrice { get; set; }

	[JsonPropertyName("market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MarketId { get; set; }

	[JsonPropertyName("tags"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Tags { get; set; }
}

public sealed class AssetResp
{
	public static AssetResp BuildFrom(AssetEntity a, MarketDef? market = null)
	{
		var aResp = new AssetResp()
		{
			Id = a.Id,
			PortfolioId = a.PortfolioId,
			ItemType = a.ItemType,
			ItemCode = a.ItemCode,
			Quantity = a.Quantity,
			AveragePrice = a.AveragePrice,
			MarketId = a.MarketId,
			Market = market != null ? MarketDefResp.BuildFrom(market) : null,
			Tags = a.Tags,
		};
		return aResp;
	}

	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; } = default!;

	[JsonPropertyName("item_type")]
	public string ItemType { get; set; } = default!;

	[JsonPropertyName("item_code")]
	public string ItemCode { get; set; } = default!;

	[JsonPropertyName("quantity")]
	public decimal Quantity { get; set; }

	[JsonPropertyName("average_price")]
	public decimal AveragePrice { get; set; }

	[JsonPropertyName("market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MarketId { get; set; }
	[JsonPropertyName("market"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MarketDefResp? Market { get; set; }

	[JsonPropertyName("tags"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Tags { get; set; }

	[JsonIgnore]
	public IEnumerable<string> TagsList => Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

	[JsonIgnore]
	public decimal TotalCost => AveragePrice * Quantity;

	public AssetEntity ToModel()
	{
		return new AssetEntity()
		{
			Id = this.Id,
			PortfolioId = this.PortfolioId,
			ItemType = this.ItemType,
			ItemCode = this.ItemCode,
			Quantity = this.Quantity,
			AveragePrice = this.AveragePrice,
			MarketId = this.MarketId,
			Tags = this.Tags,
		};
	}
}
