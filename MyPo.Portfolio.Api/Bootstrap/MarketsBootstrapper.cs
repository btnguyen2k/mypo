using System.Reflection;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api;

/// <summary>
/// Bootstrapper that loads pre-defined market data.
/// </summary>
[Bootstrapper]
public class MarketsBootstrapper
{
	private const string MARKETS_DATA_FILE = "Resources.markets.json";
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MarketsBootstrapper>();
		logger.LogInformation("Loading pre-defined markets data...");

		var assembly = Assembly.GetExecutingAssembly();
		var resourceName = $"{assembly.GetName().Name}.{MARKETS_DATA_FILE}";
		var availableResources = assembly.GetManifestResourceNames();
		if (Array.IndexOf(availableResources, resourceName) == -1)
		{
			throw new FileNotFoundException($"Markets data resource '{resourceName}' not found in assembly resources.");
		}

		using (var stream = assembly.GetManifestResourceStream(resourceName))
		{
			var marketsData = new ConfigurationBuilder()
				.AddJsonStream(stream!)
				.Build();

			Globals.Markets.Clear();
			foreach (var marketData in marketsData.GetChildren())
			{
				var marketDef = MarketDefinition.Build(marketData.Key, marketData);
				Globals.Markets.Add(marketDef);
			}
		}
	}
}
