using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<PortfolioPlanResp>>> GetMyPortfolioPlansAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<PortfolioPlanResp>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioPlanResp>> GetMyPortfolioPlanByIdAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioPlanResp>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioPlanResp>> CreatePortfolioPlanAsync(CreateOrUpdatePortfolioPlanReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Post, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLANS,
            authToken,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioPlanResp>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioPlanResp>> UpdateMyPortfolioPlanAsync(string id, CreateOrUpdatePortfolioPlanReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Put, baseUrl, endpoint,
            authToken,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioPlanResp>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioPlanResp>> DeleteMyPortfolioPlanAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Delete, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioPlanResp>(httpResult, cancellationToken);
    }
}
