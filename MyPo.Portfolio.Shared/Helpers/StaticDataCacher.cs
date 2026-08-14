using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPo.Shared.Global;

namespace MyPo.Portfolio.Shared.Helpers;

public class StaticDataCacher
{
    private static async Task CacheIndexConstituentsWithRetryAsync(HttpClient httpCLient, string index, string resourceUrl, ILogger? logger, int maxRetries = 3, int delayMs = 1000)
    {
        var indexData = GlobalRegistry.INDEX_CONSTITUENTS.GetValueOrDefault(index, new HashSet<string>());
        GlobalRegistry.INDEX_CONSTITUENTS[index] = indexData;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger?.LogInformation("Loading cached index constituents '{index}' from '{resourceUrl}'...", index, resourceUrl);
                var data = await JsonSerializer.DeserializeAsync<IDictionary<string, object>>(await httpCLient.GetStreamAsync(resourceUrl));
                var symbolList = data!["data"] as JsonElement?;
                lock (indexData)
                {
                    symbolList?.EnumerateArray().ToList().ForEach(e => indexData.Add(e.GetProperty("symbol").GetString()!));
                }
                return;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Attempt {attempt} - Failed to load cached index constituents for '{index}' from '{resourceUrl}'", attempt, index, resourceUrl);
                if (attempt == maxRetries)
                {
                    logger?.LogError("Exceeded maximum retry attempts ({maxRetries}) for loading index constituents for '{index}'", maxRetries, index);
                    return;
                }
                await Task.Delay(delayMs);
            }
        }
    }

    public static async Task CacheIndexConstituentsFinHubAsync(IServiceProvider serviceProvider, string resourcesBaseUrl)
    {
        var logger = serviceProvider.GetService<ILogger<StaticDataCacher>>();

        var httpClient = serviceProvider.GetService<HttpClient>() ?? throw new InvalidOperationException("Cannot obtain HttpClient instance.");
        resourcesBaseUrl = resourcesBaseUrl.TrimEnd('/') + '/';

        var indexMapping = new Dictionary<string, string>()
        {
            {"ASX20", $"{resourcesBaseUrl}ASX20"},
            {"ASX50", $"{resourcesBaseUrl}ASX50"},
            {"ASX100", $"{resourcesBaseUrl}ASX100"},
            {"ASX200", $"{resourcesBaseUrl}ASX200"},
            {"ASX300", $"{resourcesBaseUrl}ASX300"},
            {"HNX30", $"{resourcesBaseUrl}HNX30"},
            {"VN30", $"{resourcesBaseUrl}VN30"},
            {"VN100", $"{resourcesBaseUrl}VN100"},
            {"NASDAQ100", $"{resourcesBaseUrl}NASDAQ100"},
            {"SP500", $"{resourcesBaseUrl}SP500"},
            {"SP400", $"{resourcesBaseUrl}SP400"},
            {"SP600", $"{resourcesBaseUrl}SP600"},
        };
        foreach (var kvp in indexMapping)
        {
            var index = kvp.Key;
            var resourceUrl = kvp.Value;
            await CacheIndexConstituentsWithRetryAsync(httpClient, index, resourceUrl, logger);
        }
    }
}
