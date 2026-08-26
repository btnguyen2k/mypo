using System.Net;
using Finhub.Client;
using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseApiClient, IFinHubClient
{
    public FinHubClient(
        HttpClient httpClient,
        string baseUrl = "",
        IDictionary<string, string>? attachedHeaders = null,
        ILogger<FinHubClient>? logger = null) : base(httpClient, baseUrl, attachedHeaders, logger)
    {
        var apiKey = Environment.GetEnvironmentVariable("FINHUB_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            AddAttachedHeaders(new Dictionary<string, string>
            {
                { "X-Api-Key", apiKey },
            });
        }
    }

    private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10 * 60);
    /// <inheritdoc/>
    protected override void SetupDefaultHttpClient(HttpClient defaultHttpClient)
    {
        base.SetupDefaultHttpClient(defaultHttpClient);
        defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
    }

    private static async Task<ApiResp<T>> SendRequestAndPool<T>(
        Func<Task<HttpResponseMessage>> buildAndSendTaskRequest,
        Func<string, Task<HttpResponseMessage>> buildAndSendPollRequest,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var start = DateTimeOffset.Now;

        using var httpResultTask = await buildAndSendTaskRequest();
        var apiResultTask = await ReadAndCloseResponseAsync<T>(httpResultTask, cancellationToken: cancellationToken);
        var taskInfo = apiResultTask.ExtraAs<AsyncTaskInfo>();
        var taskId = taskInfo?.TaskId ?? string.Empty;
        if (string.IsNullOrEmpty(taskId) && apiResultTask.Status != (int)HttpStatusCode.OK)
        {
            return new ApiResp<T>()
            {
                Status = apiResultTask.IsSuccess ? (int)HttpStatusCode.InternalServerError : apiResultTask.Status,
                Message = string.IsNullOrEmpty(apiResultTask.Message)
                    ? "No task-id returned from server"
                    : apiResultTask.Message,
            };
        }

        while (apiResultTask.IsSuccess)
        {
            if (apiResultTask.Status == (int)HttpStatusCode.OK)
            {
                return apiResultTask;
            }
            if (DateTimeOffset.Now - start > timeout)
            {
                return new ApiResp<T>()
                {
                    Status = (int)HttpStatusCode.RequestTimeout,
                    Message = $"Timeout exceeded {timeout.TotalMilliseconds} ms",
                };
            }
            var delayMs = Random.Shared.Next(5000, 10000); // delay randomly 5-10 secs
            await Task.Delay(delayMs, cancellationToken: cancellationToken);
            using (var httpResultPoll = await buildAndSendPollRequest(taskId))
            {
                apiResultTask = await ReadAndCloseResponseAsync<T>(httpResultPoll, cancellationToken: cancellationToken);
            }
        }

        return apiResultTask;
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<TickerAnalysis>> AnalyzeTickerAsync(AnalyzeTickerReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var endpoint = IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER;
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Post, baseUrl, endpoint,
        //     NoAuth,
        //     req,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<TickerAnalysis>(httpResult, cancellationToken);
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER}_async";
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Post, baseUrl, endpointPoll,
                NoAuth,
                req,
                cancellationToken
            );
        }
        return await SendRequestAndPool<TickerAnalysis>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<DividendEventAnalysis>> AnalyzeDividendEventAsync(string symbol, string exDate, decimal divAmount, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var queryParams = new Dictionary<string, string?> {
        //     { "symbol", symbol },
        //     { "ex_date", exDate },
        //     { "div_amount", divAmount.ToString("F2") },
        // };
        // var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT, queryParams);
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Get, baseUrl, endpoint,
        //     NoAuth,
        //     NoData,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<DividendEventAnalysis>(httpResult, cancellationToken);
        var queryParams = new Dictionary<string, string?> {
            { "symbol", symbol },
            { "ex_date", exDate },
            { "div_amount", divAmount.ToString("F2") },
        };
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendRequestAndPool<DividendEventAnalysis>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> BuildPortfolioAsync(BuildPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var endpoint = IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO;
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Post, baseUrl, endpoint,
        //     NoAuth,
        //     req,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO}_async";
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Post, baseUrl, endpointPoll,
                NoAuth,
                req,
                cancellationToken
            );
        }
        return await SendRequestAndPool<PortfolioAnalysis>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioAsync(AnalyzePortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var endpoint = IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO;
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Post, baseUrl, endpoint,
        //     NoAuth,
        //     req,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO}_async";
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Post, baseUrl, endpointPoll,
                NoAuth,
                req,
                cancellationToken
            );
        }
        return await SendRequestAndPool<PortfolioAnalysis>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> SpotlightPortfolioAsync(SpotLightPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var endpoint = IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO;
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Post, baseUrl, endpoint,
        //     NoAuth,
        //     req,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO}_async";
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Post, baseUrl, endpointPoll,
                NoAuth,
                req,
                cancellationToken
            );
        }
        return await SendRequestAndPool<PortfolioAnalysis>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<UpcomingDividendEvent>>> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var queryParams = new Dictionary<string, string?> { { "country", country } };
        // if (!string.IsNullOrWhiteSpace(index))
        // {
        //     queryParams["index"] = index;
        // }
        // var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS, queryParams);
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Get, baseUrl, endpoint,
        //     NoAuth,
        //     NoData,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<IEnumerable<UpcomingDividendEvent>>(httpResult, cancellationToken);
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendRequestAndPool<IEnumerable<UpcomingDividendEvent>>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<UpcomingEarningsEvent>>> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var queryParams = new Dictionary<string, string?> { { "country", country } };
        // if (!string.IsNullOrWhiteSpace(index))
        // {
        //     queryParams["index"] = index;
        // }
        // var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS, queryParams);
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Get, baseUrl, endpoint,
        //     NoAuth,
        //     NoData,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<IEnumerable<UpcomingEarningsEvent>>(httpResult, cancellationToken);
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendRequestAndPool<IEnumerable<UpcomingEarningsEvent>>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<ListingEvent>>> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        // var queryParams = new Dictionary<string, string?> { { "country", country } };
        // if (!string.IsNullOrWhiteSpace(index))
        // {
        //     queryParams["index"] = index;
        // }
        // var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS, queryParams);
        // using var httpResult = await BuildAndSendRequestAsync(
        //     httpClient,
        //     HttpMethod.Get, baseUrl, endpoint,
        //     NoAuth,
        //     NoData,
        //     cancellationToken
        // );
        // return await ReadAndCloseResponseAsync<IEnumerable<ListingEvent>>(httpResult, cancellationToken);
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendRequestAndPool<IEnumerable<ListingEvent>>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<IDictionary<string, StockQuote>>> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "symbols", symbols } };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_STOCK_QUOTES, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IDictionary<string, StockQuote>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW
            .Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<SymbolOverview>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO
            .Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<SymbolInfo>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, string date, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT
            .Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", date, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<HistoryPoint>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<HistoryPoint>>> GetStockQuoteHistoryAsync(string symbol, int? days = 100, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "days", days?.ToString() ?? "100" },
        };
        var endpoint = QueryHelpers.AddQueryString(
            IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_HISTORY
                .Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase),
            queryParams
        );
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<HistoryPoint>>(httpResult, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<StockQuote>> GetGoldQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "currency", currency ?? "USD" } };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_TOZ_GOLD_QUOTE, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<StockQuote>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<HistoryPoint>>> GetGoldPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "currency", currency??"USD" },
            { "days", days?.ToString() ?? "30" },
        };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_TOZ_GOLD_HISTORY, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<HistoryPoint>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<StockQuote>> GetSilverQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "currency", currency ?? "USD" } };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_TOZ_SILVER_QUOTE, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<StockQuote>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<HistoryPoint>>> GetSilverPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "currency", currency??"USD" },
            { "days", days?.ToString() ?? "30" },
        };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_TOZ_SILVER_HISTORY, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<HistoryPoint>>(httpResult, cancellationToken);
    }
}
