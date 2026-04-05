using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class AIVendor
{
	public const string VENDOR_GEMINI = "Gemini";
	public const string VENDOR_OPENAI = "OpenAI";
	public const string VENDOR_AZURE_OPENAI = "AzureOpenAI";

	public const string TIER_FREE = "FreeTier";
	public const string TIER_LOW_COST = "LowCostTier";
	public const string TIER_PREMIUM = "PremiumTier";

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("tiered_models")]
	public Dictionary<string, List<string>> TieredModels { get; set; } = [];

	// [JsonPropertyName("capacity"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonIgnore]
	public AICapacity? Capacity { get; set; }
}

public class AICapacity
{
	public const string API_TYPE_CHAT = "Chat";
	public const string API_TYPE_RESPONSE = "Response";

	private readonly Dictionary<string, AIModelCapacity> CapacityPerModel = [];

	public AICapacity(IConfiguration config)
	{
		config.GetChildren().ToList().ForEach(section =>
		{
			var modelName = section.Key;
			var apiType = section.GetValue<string>("ApiType") ?? API_TYPE_CHAT;
			var toolChain = section.GetSection("ToolChain").Get<List<Dictionary<string, string>>>() ?? [];
			CapacityPerModel[modelName] = new AIModelCapacity
			{
				ApiType = apiType,
				ToolChain = toolChain,
			};
		});
	}

	public string GetApiTypeForModel(string modelOrDeployment)
	{
		if (!CapacityPerModel.TryGetValue(modelOrDeployment, out var modelCapacity))
		{
			return API_TYPE_CHAT; // Default to Chat API if capacity info is not available for the model
		}
		return modelCapacity.ApiType;
	}

	public List<Dictionary<string, string>> GetToolChainForModel(string modelOrDeployment)
	{
		return CapacityPerModel.TryGetValue(modelOrDeployment, out var modelCapacity) ? modelCapacity.ToolChain : [];
	}
}

sealed class AIModelCapacity
{
	public string ApiType { get; set; } = AICapacity.API_TYPE_CHAT;
	public List<Dictionary<string, string>> ToolChain { get; set; } = [];
}

public sealed class DividendEventAnalysis
{
	/* base info */

	[JsonPropertyName("Overview"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SymbolOverview? Overview { get; set; }

	[JsonPropertyName("price")]
	public decimal Price { get; set; }

	[JsonPropertyName("ex_div_date_timestamp")]
	public int ExDivTimestamp { get; set; }

	[JsonPropertyName("ex_div_date")]
	public string ExDivTimestampStr { get; set; } = string.Empty;

	[JsonIgnore]
	public DateTimeOffset ExDivDate => !string.IsNullOrEmpty(ExDivTimestampStr)
		? DateTimeOffset.TryParse(ExDivTimestampStr, out var dt) ? dt.ToUniversalTime() : DateTimeOffset.FromUnixTimeSeconds(ExDivTimestamp).ToUniversalTime()
		: DateTimeOffset.FromUnixTimeSeconds(ExDivTimestamp).ToUniversalTime();

	[JsonPropertyName("div_amount")]
	public decimal DivAmount { get; set; }

	[JsonPropertyName("div_yield")]
	public decimal DivYield { get; set; }

	/* analysis result */

	[JsonPropertyName("num_samples")]
	public int NumSamples { get; set; }

	[JsonPropertyName("drop_price_min")]
	public decimal DropPriceMin { get; set; }

	[JsonPropertyName("drop_price_max")]
	public decimal DropPriceMax { get; set; }

	[JsonIgnore]
	public decimal DropPriceMean => (DropPriceMin + DropPriceMax) / 2;

	[JsonPropertyName("recovery_probability")]
	public decimal RecoveryProb { get; set; }

	[JsonPropertyName("recovery_days_min")]
	public int RecoveryDaysMin { get; set; }

	[JsonPropertyName("recovery_days_max")]
	public int RecoveryDaysMax { get; set; }

	[JsonPropertyName("recovery_price_min")]
	public decimal RecoveryPriceMin { get; set; }

	[JsonPropertyName("recovery_price_max")]
	public decimal RecoveryPriceMax { get; set; }

	[JsonIgnore]
	public decimal RecoveryPriceMean => (RecoveryPriceMin + RecoveryPriceMax) / 2;

	/* technical data, used for further analysis with AI */

    /* analysis result from AI */

	[JsonPropertyName("llm_error")]
	public bool LLMError { get; set; } = false;

	[JsonPropertyName("llm_error_msg"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? LLMErrorMsg { get; set; }

	[JsonPropertyName("search_summary"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? SearchSummary { get; set; }

	[JsonPropertyName("strategy"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Strategy { get; set; }

	[JsonPropertyName("reasoning"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Reasoning { get; set; }

	[JsonPropertyName("sentiment_score")]
	public decimal SentimentScore { get; set; }

	[JsonPropertyName("recovery_probability_adj")]
	public decimal RecoveryProbAdj { get; set; }

	[JsonPropertyName("recovery_days_adj"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RecoveryDaysAdj { get; set; }

	[JsonPropertyName("drop_price_adj"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DropPriceAdj { get; set; }

	[JsonPropertyName("recovery_price_adj"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RecoveryPriceAdj { get; set; }

	[JsonPropertyName("expected_pl")]
	public decimal ExpectedPL { get; set; }

	[JsonPropertyName("confidence_level")]
	public decimal ConfidenceLevel { get; set; }

	[JsonPropertyName("risk_level")]
	public decimal RiskLevel { get; set; }
}
