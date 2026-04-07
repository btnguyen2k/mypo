using MyPo.Shared.Bootstrap;
using System.Reflection;
using System.Text.Json;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that registers services used by Blazor application.
/// </summary>
/// <remarks>
///		This bootstrapper is shared between Blazor Server and Blazor WASM envs.
/// </remarks>
[Bootstrapper]
public class StaticDataBootstrapper
{
	public static void InitializeServices(IServiceProvider _, ILogger<StaticDataBootstrapper> logger)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var indexMapping = new Dictionary<string, string>()
		{
			{"ASX20", "Resources.indices.asx20.json"},
			{"ASX50", "Resources.indices.asx50.json"},
			{"ASX100", "Resources.indices.asx100.json"},
			{"ASX200", "Resources.indices.asx200.json"},
			{"ASX300", "Resources.indices.asx300.json"},
			{"HNX30", "Resources.indices.hnx30.json"},
			{"VN30", "Resources.indices.vn30.json"},
			{"VN100", "Resources.indices.vn100.json"},
			{"NASDAQ100", "Resources.indices.nasdaq100.json"},
			{"SP500", "Resources.indices.sp500.json"},
			{"SP400", "Resources.indices.spmidcap400.json"},
			{"SP600", "Resources.indices.spsmallcap600.json"},
		};
		var availableResources = assembly.GetManifestResourceNames();
		foreach (var kvp in indexMapping)
		{
			var indexData = new HashSet<string>();
			Globals.IndexConstituents[kvp.Key] = indexData;

			var index = kvp.Key;
			var resourceName = $"{assembly.GetName().Name}.{kvp.Value}";
			if (Array.IndexOf(availableResources, resourceName) == -1)
			{
				throw new FileNotFoundException($"Index data resource '{resourceName}' not found in assembly resources.");
			}

			logger.LogInformation("Loading cached index constituents '{index}' from '{resourceName}'...", index, resourceName);
			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				using (var reader = new StreamReader(stream!))
				{
					var data = JsonSerializer.Deserialize<IDictionary<string, object>>(reader.ReadToEnd());
					var symbolList = data!["data"] as JsonElement?;
					symbolList?.EnumerateArray().ToList().ForEach(e => indexData.Add(e.GetProperty("symbol").GetString()!));
				}
			}
		}
	}
}
