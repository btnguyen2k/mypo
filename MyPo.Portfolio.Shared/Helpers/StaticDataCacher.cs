using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPo.Shared.Global;

namespace MyPo.Portfolio.Shared.Helpers;

public class StaticDataCacher
{
	public static async Task CacheIndexConstituentsAsync(IServiceProvider serviceProvider, string resourcesBaseUrl)
	{
		var logger = serviceProvider.GetService<ILogger<StaticDataCacher>>();

		var httpClient = serviceProvider.GetService<HttpClient>() ?? throw new InvalidOperationException("Cannot obtain HttpClient instance.");

		var indexMapping = new Dictionary<string, string>()
		{
			{"ASX20", $"{resourcesBaseUrl}asx20.json"},
			{"ASX50", $"{resourcesBaseUrl}asx50.json"},
			{"ASX100", $"{resourcesBaseUrl}asx100.json"},
			{"ASX200", $"{resourcesBaseUrl}asx200.json"},
			{"ASX300", $"{resourcesBaseUrl}asx300.json"},
			{"HNX30", $"{resourcesBaseUrl}hnx30.json"},
			{"VN30", $"{resourcesBaseUrl}vn30.json"},
			{"VN100", $"{resourcesBaseUrl}vn100.json"},
			{"NASDAQ100", $"{resourcesBaseUrl}nasdaq100.json"},
			{"SP500", $"{resourcesBaseUrl}sp500.json"},
			{"SP400", $"{resourcesBaseUrl}spmidcap400.json"},
			{"SP600", $"{resourcesBaseUrl}spsmallcap600.json"},
		};
		foreach (var kvp in indexMapping)
		{
			var indexData = new HashSet<string>();
			GlobalRegistry.INDEX_CONSTITUENTS[kvp.Key] = indexData;

			var index = kvp.Key;
			var resourceName = kvp.Value;
			logger?.LogInformation("Loading cached index constituents '{index}' from '{resourceName}'...", index, resourceName);
			try
			{
				var data = await JsonSerializer.DeserializeAsync<IDictionary<string, object>>(await httpClient.GetStreamAsync(resourceName));
				var symbolList = data!["data"] as JsonElement?;
				symbolList?.EnumerateArray().ToList().ForEach(e => indexData.Add(e.GetProperty("symbol").GetString()!));
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Failed to load cached index constituents for '{index}' from '{resourceName}'", index, resourceName);
			}
		}
	}
}
