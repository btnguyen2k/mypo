using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPo.Shared.Global;

namespace MyPo.Portfolio.Shared.Helpers;

public class StaticDataCacher
{
    private static readonly TimeSpan perAttemptTimeout = TimeSpan.FromSeconds(10);

    private static async Task CacheIndexConstituentsWithRetryAsync(
        HttpClient httpClient,
        string index,
        string resourceUrl,
        ILogger? logger,
        int maxRetries = 3,
        int delayMs = 1000,
        CancellationToken cancellationToken = default)
    {
        var resolvedDelayMs = delayMs;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger?.LogInformation(
                    "Loading index constituents '{index}' from '{resourceUrl}', attempt {attempt}...",
                    index,
                    resourceUrl,
                    attempt);

                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptCts.CancelAfter(perAttemptTimeout);

                using var response = await httpClient.GetAsync(
                    resourceUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    attemptCts.Token);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync(attemptCts.Token);
                var data = await JsonSerializer.DeserializeAsync<IDictionary<string, object>>(
                    responseStream,
                    cancellationToken: attemptCts.Token);

                var loadedSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var symbolList = data!["data"] as JsonElement?;
                foreach (var element in symbolList?.EnumerateArray() ?? [])
                {
                    var symbol = element.GetProperty("symbol").GetString();
                    if (!string.IsNullOrWhiteSpace(symbol))
                    {
                        loadedSymbols.Add(symbol);
                    }
                }
                GlobalRegistry.INDEX_CONSTITUENTS[index] = loadedSymbols;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Overall budget expired or the application is stopping.
                throw;
            }
            catch (OperationCanceledException ex)
            {
                logger?.LogWarning(
                    ex,
                    "Attempt {attempt} timed out after {timeoutSeconds} seconds for '{index}'",
                    attempt,
                    perAttemptTimeout.TotalSeconds,
                    index);
            }
            catch (HttpRequestException ex)
            {
                logger?.LogWarning(
                    ex,
                    "Attempt {attempt} failed for '{index}' from '{resourceUrl}'",
                    attempt,
                    index,
                    resourceUrl);
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(resolvedDelayMs, cancellationToken);
                // backoff exponentially
                resolvedDelayMs = (int)(resolvedDelayMs * 1.2);
            }
        }

        logger?.LogError("Exceeded maximum retry attempts ({maxRetries}) for '{index}'", maxRetries, index);
    }

    public static async Task CacheIndexConstituentsFinHubAsync(
        IServiceProvider serviceProvider,
        string resourcesBaseUrl,
        CancellationToken cancellationToken = default)
    {
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

        var logger = serviceProvider.GetService<ILogger<StaticDataCacher>>();
        var httpClient = serviceProvider.GetService<HttpClient>() ?? throw new InvalidOperationException("Cannot obtain HttpClient instance.");

        using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var maxBudgetSeconds = Math.Min(perAttemptTimeout.Seconds * indexMapping.Count, 60);
        budgetCts.CancelAfter(TimeSpan.FromSeconds(maxBudgetSeconds));

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = budgetCts.Token,
        };

        try
        {
            await Parallel.ForEachAsync(
                indexMapping,
                parallelOptions,
                async (entry, operationToken) => await CacheIndexConstituentsWithRetryAsync(
                    httpClient,
                    entry.Key,
                    entry.Value,
                    logger,
                    cancellationToken: operationToken));
        }
        catch (OperationCanceledException) when (budgetCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning("Index constituent loading exceeded its total {timeoutSeconds}-second budget", maxBudgetSeconds);
        }
    }
}
