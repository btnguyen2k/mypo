using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdatePortfolioPlanReq
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }

	[JsonPropertyName("portfolio_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PortfolioId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioPlanMetadata? Metadata { get; set; }
}

public sealed class PortfolioPlanResp
{
	public static PortfolioPlanResp BuildFrom(PortfolioPlanEntity pr)
	{
		return new PortfolioPlanResp
		{
			Id = pr.Id,
			OwnerUserId = pr.OwnerUserId,
			PortfolioId = pr.PortfolioId,
			Name = pr.Name,
			Metadata = pr.Metadata,
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("owner_id")]
	public string OwnerUserId { get; set; } = default!;

	[JsonPropertyName("portfolio_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PortfolioId { get; set; }

	[JsonPropertyName("portfolio"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioResp? Portfolio { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioPlanMetadata? Metadata { get; set; }

	[JsonIgnore]
	public MarketDefResp? Market { get; set; }

	[JsonIgnore]
	public string HoldingSymbols => Metadata?.HoldingTickers.Select(ht => ht.Ticker).Aggregate((a, b) => $"{a}, {b}") ?? string.Empty;

	[JsonIgnore]
	public decimal TotalMarketValue => Metadata?.HoldingTickers.Sum(ht => ht.Shares * ht.MarketPrice) ?? 0;

	public string TotalMarketValueStr(MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol??""}{FormatUtils.FormatValueWithScale(TotalMarketValue, m?.PriceScale??1, m?.ValueFormat??"")}";
	}
}
