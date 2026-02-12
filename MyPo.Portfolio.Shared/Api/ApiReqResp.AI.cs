using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Api;

public sealed class SymbolAnalysisReq
{
	[JsonPropertyName("ai_vendor")]
	public string AIVendor { get; set; } = Models.FinHub.AIVendor.VENDOR_GEMINI;

	[JsonPropertyName("tier")]
	public string Tier { get; set; } = Models.FinHub.AIVendor.TIER_FREE;

	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("max_output_tokens")]
	public int MaxOutputTokens { get; set; } = 3000;

	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("inputs")]
	public string Inputs { get; set; } = string.Empty;

	[JsonPropertyName("expected_outputs")]
	public string ExpectedOutputs { get; set; } = string.Empty;

	[JsonPropertyName("output_format")]
	public string OutputFormat { get; set; } = "Markdown";
}

public struct SymbolAnalysisResp
{
	[JsonPropertyName("response")]
	public string Response { get; set; }
}
