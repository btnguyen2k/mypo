using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdateTransactionRecReq
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string Id { get; set; }
	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("time")]
	public DateTimeOffset Time { get; set; }

	[JsonPropertyName("quantity")]
	public decimal Quantity { get; set; }

	[JsonPropertyName("price")]
	public decimal Price { get; set; }

	[JsonPropertyName("fee_tx"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal? FeeTx { get; set; }

	[JsonPropertyName("fee_tax"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal? FeeTax { get; set; }

	[JsonPropertyName("fee_other"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal? FeeOther { get; set; }

	[JsonPropertyName("item_type")]
	public string ItemType { get; set; }

	[JsonPropertyName("item_code")]
	public string ItemCode { get; set; }

	[JsonPropertyName("market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MarketId { get; set; }

	[JsonPropertyName("notes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Notes { get; set; }

	[JsonPropertyName("is_settled")]
	public bool IsSettled { get; set; }
}

public struct TransactionRecResp
{
	public static TransactionRecResp BuildFrom(TransactionRec tr, MarketDef? market = null)
	{
		var txRecResp = new TransactionRecResp()
		{
			Id = tr.Id,
			PortfolioId = tr.PortfolioId,
			Type = tr.Type,
			Time = tr.Time,
			Quantity = tr.Quantity,
			Price = tr.Price,
			FeeTx = tr.FeeTx,
			FeeTax = tr.FeeTax,
			FeeOther = tr.FeeOther,
			ItemType = tr.ItemType,
			ItemCode = tr.ItemCode,
			MarketId = tr.MarketId,
			Market = market != null ? MarketDefResp.BuildFrom(market) : null,
			Notes = tr.Notes,
			IsSettled = tr.IsSettled,
		};
		if (market!=null)
		{
			txRecResp.Time = TimeZoneInfo.ConvertTime(tr.Time, market.TZ);
		}
		return txRecResp;
	}

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("time")]
	public DateTimeOffset Time { get; set; }

	[JsonPropertyName("quantity")]
	public decimal Quantity { get; set; }

	[JsonPropertyName("price")]
	public decimal Price { get; set; }

	[JsonPropertyName("fee_tx"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal FeeTx { get; set; }

	[JsonPropertyName("fee_tax"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal FeeTax { get; set; }

	[JsonPropertyName("fee_other"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public decimal FeeOther { get; set; }

	[JsonPropertyName("item_type")]
	public string ItemType { get; set; }

	[JsonPropertyName("item_code")]
	public string ItemCode { get; set; }

	[JsonPropertyName("market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MarketId { get; set; }
	[JsonPropertyName("market"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MarketDefResp? Market { get; set; }

	[JsonPropertyName("notes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Notes { get; set; }

	[JsonPropertyName("is_settled")]
	public bool IsSettled { get; set; }
}
