using Finhub.Client;
using FinHub.Client.Schemas.Stocks;
using Microsoft.AspNetCore.WebUtilities;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient
{
    /// <inheritdoc/>
    public async Task<GetStockQuotesResponse> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockQuotesResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetSymbolOverviewResponse> GetStockSymbolOverviewAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetSymbolOverviewResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetSymbolInfoResponse> GetStockSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetSymbolInfoResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetStockQuoteAtDateResponse> GetStockQuoteAtDateAsync(string symbol, string date, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockQuoteAtDateResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetStockHistoryResponse> GetStockQuoteHistoryAsync(string symbol, int? days = 100, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockHistoryResponse>(httpResult, cancellationToken);
    }
}
