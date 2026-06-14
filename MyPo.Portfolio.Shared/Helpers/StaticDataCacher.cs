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
            var index = kvp.Key;
            var resourceUrl = kvp.Value;
            await CacheIndexConstituentsWithRetryAsync(httpClient, index, resourceUrl, logger);
        }
    }
}
