using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct RoiRecResp
{
	public static RoiRecResp BuildFrom(RoiRec r, MarketDef? market = null)
	{
		var roiResp = new RoiRecResp()
		{
			Id = r.Id,
			Status = r.Status,
			PortfolioId = r.PortfolioId,
			TxType = r.TxType,
			TxTime = r.TxTime,
			TxValue = r.TxValue,
			TxDesc = r.TxDesc,
			RefTxId = r.RefTxId,
			RefItemType = r.RefItemType,
			RefItemCode = r.RefItemCode,
			RefMarketId = r.RefMarketId,
			Market = market != null ? MarketDefResp.BuildFrom(market) : null
		};
		if (market!=null)
		{
			roiResp.TxTime = TimeZoneInfo.ConvertTime(roiResp.TxTime, market.TZ);
		}
		return roiResp;
	}

	[JsonPropertyName("id")]
	public string Id { get; set; }
	[JsonPropertyName("status")]
	public string Status { get; set; }

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("tx_type")]
	public string TxType { get; set; }
	[JsonPropertyName("tx_time")]
	public DateTimeOffset TxTime { get; set; }
	[JsonPropertyName("tx_value")]
	public decimal TxValue { get; set; }
	[JsonPropertyName("tx_desc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TxDesc { get; set; }

	[JsonPropertyName("ref_tx_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefTxId { get; set; }
	[JsonPropertyName("ref_item_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemType { get; set; }
	[JsonPropertyName("ref_item_code"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemCode { get; set; }
	[JsonPropertyName("ref_market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefMarketId { get; set; }
	[JsonPropertyName("market"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MarketDefResp? Market { get; set; }
}
