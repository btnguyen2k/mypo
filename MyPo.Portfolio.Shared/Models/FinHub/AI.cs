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

public class EventBase
{
	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("company_name")]
	public string CompanyName { get; set; } = string.Empty;

	[JsonPropertyName("timestamp")]
	public int Timestamp { get; set; }

	[JsonIgnore]
	public DateTimeOffset Date => DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToUniversalTime();

	[JsonPropertyName("event_category")]
	public string EventCategory { get; set; } = string.Empty;

	[JsonPropertyName("source_name")]
	public string SourceName { get; set; } = string.Empty;

	[JsonPropertyName("link")]
	public string Link { get; set; } = string.Empty;
}

public sealed class IncomingEarningsEvent : EventBase
{
	[JsonPropertyName("report_period")]
	public string ReportPeriod { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;
}

public sealed class IncomingDividendEvent : EventBase
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("amount")]
	public decimal Amount { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;
}
