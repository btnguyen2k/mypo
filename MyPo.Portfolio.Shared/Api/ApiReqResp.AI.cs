using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Api;

public sealed class SymbolAnalysisReq
{
	[JsonPropertyName("ai_vendor")]
	public string AIVendor { get; set; } = Models.FinHub.AIVendor.VENDOR_GEMINI;

	[JsonPropertyName("ai_tier")]
	public string AITier { get; set; } = Models.FinHub.AIVendor.TIER_FREE;

	[JsonPropertyName("ai_model")]
	public string AIModel { get; set; } = string.Empty;

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

	[JsonPropertyName("num_tokens_prompt")]
	public int NumTokensPrompt { get; set; }

	[JsonPropertyName("num_tokens_thought")]
	public int NumTokensThought { get; set; }

	[JsonPropertyName("num_tokens_response")]
	public int NumTokensResponse { get; set; }

	[JsonIgnore]
	public readonly int TotalTokens => NumTokensPrompt + NumTokensThought + NumTokensResponse;

	[JsonPropertyName("total_time_ms")]
	public int TotalTimeMs { get; set; }

	[JsonPropertyName("is_cached")]
	public bool IsCached { get; set; }
}
