using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseClient, IFinHubClient
{
    public FinHubClient(ILogger<FinHubClient> logger, HttpClient httpClient, string baseUrl = "") : base(logger, httpClient, baseUrl)
    {
        var apiKey = Environment.GetEnvironmentVariable("FINHUB_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            AddDefaultHeaders(new Dictionary<string, string>
            {
                { "X-Api-Key", apiKey },
            });
        }
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<TickerAnalysis>> AnalyzeTickerAsync(AnalyzeTickerReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER;
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<TickerAnalysis>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<DividendEventAnalysis>> AnalyzeDividendEventAsync(string symbol, string exDate, decimal divAmount, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "symbol", symbol },
            { "ex_date", exDate },
            { "div_amount", divAmount.ToString("F2") },
        };
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<DividendEventAnalysis>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> BuildPortfolioAsync(BuildPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO;
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioAsync(AnalyzePortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO;
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> SpotlightPortfolioAsync(SpotLightPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO;
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<UpcomingDividendEvent>>> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<UpcomingDividendEvent>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<UpcomingEarningsEvent>>> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<UpcomingEarningsEvent>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<ListingEvent>>> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<ListingEvent>>(httpResult, cancellationToken);
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
