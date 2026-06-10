using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Api;

public sealed class TickerAnalysisReq
{
	[JsonPropertyName("max_output_tokens")]
	public int MaxOutputTokens { get; set; }

	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("portfolio_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PortfolioId { get; set; }

	[JsonPropertyName("intent")]
	public string Intent { get; set; } = string.Empty;
}
