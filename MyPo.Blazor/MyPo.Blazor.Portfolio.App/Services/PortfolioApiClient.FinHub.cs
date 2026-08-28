using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO
            .Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<PortfolioAnalysis>> SpotlightPortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO
            .Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc />
    public async Task<ApiResp<TickerAnalysis>> AnalyzeTickerAsync(TickerAnalysisReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_ANALYZE_TICKER;
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Post, baseUrl, endpoint,
            authToken,
            req,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<TickerAnalysis>(httpResult, cancellationToken);
    }
}
