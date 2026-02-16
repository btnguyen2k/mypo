using Google.GenAI;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Bootstrap;
using System.Reflection;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class AIClientsBootstrapper
{
	private const string EXTERNAL_SERVICES_SETTINGS_FILE = "Resources.ai_clients_settings.json";
	private static readonly ILogger<AIClientsBootstrapper> logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AIClientsBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		logger.LogInformation("Loading AI clients settings...");
		var assembly = Assembly.GetExecutingAssembly();
		var resourceName = $"{assembly.GetName().Name}.{EXTERNAL_SERVICES_SETTINGS_FILE}";
		var availableResources = assembly.GetManifestResourceNames();
		if (Array.IndexOf(availableResources, resourceName) == -1)
		{
			throw new FileNotFoundException($"External services settings resource '{resourceName}' not found in assembly resources.");
		}
		using (var stream = assembly.GetManifestResourceStream(resourceName))
		{
			var externalServicesSettings = new ConfigurationBuilder()
				.AddJsonStream(stream!)
				.AddEnvironmentVariables()
				.Build();
			ConfigureAzureOpenAIClient(appBuilder.Services, externalServicesSettings);
			ConfigureGeminiClient(appBuilder.Services, externalServicesSettings);
			ConfigureOpenAIClient(appBuilder.Services, externalServicesSettings);

			foreach (var v in Globals.AIVendors)
			{
				Globals.AIVendorsMap[v.Name.ToUpper()] = v;
			}
		}
	}

	private static void ConfigureAzureOpenAIClient(IServiceCollection services, IConfiguration aiClientsSettings)
	{
		const string AI_VENDOR = AIVendor.VENDOR_AZURE_OPENAI;
		var aiVendor = new AIVendor
		{
			Name = AI_VENDOR,
			TieredModels = [],
		};

		foreach (var aiTier in new[] { AIVendor.TIER_FREE, AIVendor.TIER_LOW_COST, AIVendor.TIER_PREMIUM })
		{
			var key = $"{AI_VENDOR}:{aiTier}";
			logger.LogInformation("Configuring {vendor} client for '{key}'...", AI_VENDOR, key);
			var apiEndpoint = aiClientsSettings.GetValue<string>($"{key}:Endpoint");
			var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
			var availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
			if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
			{
				logger.LogWarning("API Endpoint/Key and/or available models for '{key}' is not configured. {vendor} client for this tier will not be available.", key, AI_VENDOR);
			}
			else
			{
				logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
				aiVendor.TieredModels[aiTier] = availableModels;
				services.AddKeyedSingleton<OpenAIChatClientFactory, OpenAIChatClientFactory>(key, (sp, key) =>
				{
					var apiEndpoint = aiClientsSettings.GetValue<string>($"{key}:Endpoint") ?? "No endpoint provided";
					var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
					return new OpenAIChatClientFactory(apiKey: apiKey, endpoint: apiEndpoint);
				});
			}
		}

		if (aiVendor.TieredModels.Count > 0) Globals.AIVendors.Add(aiVendor);
	}

	private static void ConfigureGeminiClient(IServiceCollection services, IConfiguration aiClientsSettings)
	{
		const string AI_VENDOR = AIVendor.VENDOR_GEMINI;
		var aiVendor = new AIVendor
		{
			Name = AI_VENDOR,
			TieredModels = [],
		};

		foreach (var aiTier in new[] { AIVendor.TIER_FREE, AIVendor.TIER_LOW_COST, AIVendor.TIER_PREMIUM })
		{
			var key = $"{AI_VENDOR}:{aiTier}";
			logger.LogInformation("Configuring {vendor} client for '{key}'...", AI_VENDOR, key);
			var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
			var availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
			if (string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
			{
				logger.LogWarning("API Key and/or available models for '{key}' is not configured. {vendor} client for this tier will not be available.", key, AI_VENDOR);
			}
			else
			{
				logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
				aiVendor.TieredModels[aiTier] = availableModels;
				services.AddKeyedSingleton<Client, Client>(key, (sp, key) =>
				{
					var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
					return new Client(apiKey: apiKey);
				});
			}
		}

		if (aiVendor.TieredModels.Count > 0) Globals.AIVendors.Add(aiVendor);
	}

	private static void ConfigureOpenAIClient(IServiceCollection services, IConfiguration aiClientsSettings)
	{
		const string AI_VENDOR = AIVendor.VENDOR_OPENAI;
		var aiVendor = new AIVendor
		{
			Name = AI_VENDOR,
			TieredModels = [],
		};

		foreach (var aiTier in new[] { AIVendor.TIER_FREE, AIVendor.TIER_LOW_COST, AIVendor.TIER_PREMIUM })
		{
			var key = $"{AI_VENDOR}:{aiTier}";
			logger.LogInformation("Configuring {vendor} client for '{key}'...", AI_VENDOR, key);
			var apiEndpoint = aiClientsSettings.GetValue<string>($"{key}:Endpoint");
			var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
			var availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
			if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
			{
				logger.LogWarning("API Endpoint/Key and/or available models for '{key}' is not configured. {vendor} client for this tier will not be available.", key, AI_VENDOR);
			}
			else
			{
				logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
				aiVendor.TieredModels[aiTier] = availableModels;
				services.AddKeyedSingleton<OpenAIChatClientFactory, OpenAIChatClientFactory>(key, (sp, key) =>
				{
					var apiEndpoint = aiClientsSettings.GetValue<string>($"{key}:Endpoint") ?? "No endpoint provided";
					var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
					return new OpenAIChatClientFactory(apiKey: apiKey, endpoint: apiEndpoint);
				});
			}
		}

		if (aiVendor.TieredModels.Count > 0) Globals.AIVendors.Add(aiVendor);
	}
}
