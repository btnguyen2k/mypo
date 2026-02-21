using System.Text.Json.Serialization;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class SymbolAnalysisEntity : Entity<string>
{
	public const string ANALYSIS_TYPE_FULL = "FULL";

	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	public string OwnerId { get; set; } = string.Empty;

	public string MarketId { get; set; } = string.Empty;

	public string ItemType { get; set; } = string.Empty;

	public string ItemCode { get; set; } = string.Empty;

	public string AnalysisType { get; set; } = ANALYSIS_TYPE_FULL;

	public DateTimeOffset AnalysisTime { get; set; } = DateTimeOffset.UtcNow;

	public string? AnalysisPrompt { get; set; }

	public string? AnalysisResult { get; set; }

	public SymbolAnalysisMetadata? Metadata { get; set; }
}

public sealed class SymbolAnalysisMetadata
{
	[JsonPropertyName("ai_vendor")]
	public string AIVendor { get; set; } = string.Empty;

	[JsonPropertyName("ai_tier")]
	public string AITier { get; set; } = string.Empty;

	[JsonPropertyName("ai_model")]
	public string AIModel { get; set; } = string.Empty;

	[JsonPropertyName("total_time_ms")]
	public int TotalTimeMs { get; set; }

	[JsonPropertyName("prompt_tokens")]
	public int PromptTokens { get; set; }

	[JsonPropertyName("completion_tokens")]
	public int CompletionTokens { get; set; }

	[JsonPropertyName("thought_tokens")]
	public int ThoughtTokens { get; set; }

	[JsonIgnore]
	public int TotalTokens => PromptTokens + CompletionTokens + ThoughtTokens;

	[JsonPropertyName("prompt_cost")]
	public decimal PromptCost { get; set; }

	[JsonPropertyName("completion_cost")]
	public decimal CompletionCost { get; set; }

	[JsonPropertyName("thought_cost")]
	public decimal TotalCost { get; set; }
}
