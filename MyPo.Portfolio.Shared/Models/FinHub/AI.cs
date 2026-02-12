using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class AIVendor
{
	public const string VENDOR_GEMINI = "Gemini";
	public const string VENDOR_OPENAI = "OpenAI";

	public const string TIER_FREE = "FreeTier";
	public const string TIER_LOW_COST = "LowCostTier";
	public const string TIER_PREMIUM = "PremiumTier";

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("tiered_models")]
	public Dictionary<string, List<string>> TieredModels { get; set; } = [];
}
