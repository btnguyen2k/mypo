using Google.GenAI;
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
			ConfigureGeminiClient(appBuilder.Services, externalServicesSettings);
		}
	}

	private static void ConfigureGeminiClient(IServiceCollection services, IConfiguration aiClientsSettings)
	{
		var aiVendor = new AIVendor
		{
			Name = AIVendor.VENDOR_GEMINI,
			TieredModels = [],
		};
		Globals.AIVendors.Add(aiVendor);

		// free tier
		var key = "Gemini:FreeTier";
		logger.LogInformation("Configuring Gemini client for '{Key}'...", key);
		var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
		var availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
		if (string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
		{
			logger.LogWarning("API key and/or available models for '{Key}' is not configured. Gemini client for this tier will not be available.", key);
		}
		else
		{
			logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
			aiVendor.TieredModels[AIVendor.TIER_FREE] = availableModels;
			services.AddKeyedSingleton<Client, Client>(key, (sp, key) =>
			{
				var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
				return new Client(apiKey: apiKey);
			});
		}

		// low-cost tier
		key = "Gemini:LowCostTier";
		logger.LogInformation("Configuring Gemini client for '{Key}'...", key);
		apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
		availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
		if (string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
		{
			logger.LogWarning("API key and/or available models for '{Key}' is not configured. Gemini client for this tier will not be available.", key);
		}
		else
		{
			logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
			aiVendor.TieredModels[AIVendor.TIER_LOW_COST] = availableModels;
			services.AddKeyedSingleton<Client, Client>(key, (sp, key) =>
			{
				var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
				return new Client(apiKey: apiKey);
			});
		}

		// premium tier
		key = "Gemini:PremiumTier";
		logger.LogInformation("Configuring Gemini client for '{Key}'...", key);
		apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey");
		availableModels = aiClientsSettings.GetSection($"{key}:Models").Get<List<string>>() ?? [];
		if (string.IsNullOrEmpty(apiKey) || availableModels.Count == 0)
		{
			logger.LogWarning("API key and/or available models for '{Key}' is not configured. Gemini client for this tier will not be available.", key);
		}
		else
		{
			logger.LogInformation("-- Available models for '{key}': {models}.", key, string.Join(", ", availableModels));
			aiVendor.TieredModels[AIVendor.TIER_PREMIUM] = availableModels;
			services.AddKeyedSingleton<Client, Client>(key, (sp, key) =>
			{
				var apiKey = aiClientsSettings.GetValue<string>($"{key}:ApiKey") ?? "No key provided";
				return new Client(apiKey: apiKey);
			});
		}

		foreach (var v in Globals.AIVendors)
		{
			Globals.AIVendorsMap[v.Name.ToUpper()] = v;
		}
	}
}
