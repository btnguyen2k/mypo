using Finhub.Client;
using FinHub.Client.Schemas.Stocks;
using Microsoft.AspNetCore.WebUtilities;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient
{
    /// <inheritdoc/>
    public async Task<GetStockQuoteResponse> GetGoldQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockQuoteResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetStockHistoryResponse> GetGoldPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockHistoryResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetStockQuoteResponse> GetSilverQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockQuoteResponse>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetStockHistoryResponse> GetSilverPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsApiRespAsync<GetStockHistoryResponse>(httpResult, cancellationToken);
    }
}
