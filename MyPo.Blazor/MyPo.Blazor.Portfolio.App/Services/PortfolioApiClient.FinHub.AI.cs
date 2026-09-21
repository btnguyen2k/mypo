using FinHub.Client.Models.Portfolios;
using FinHub.Client.Models.Tickers;
using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Schemas.PortfolioSpotlight;
using FinHub.Client.Schemas.TickerAnalysis;
using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
    /// <inheritdoc/>
    public async Task<AnalyzePortfolioResponse> AnalyzePortfolioPlanAsync(
        string planId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        return await SendApiRequestAndPollAsync<AnalyzePortfolioAsyncResponse, AnalyzePortfolioResponse, IPortfolioAnalysisResult>(
            endpoint,
            NoData,
            MIN_TIMEOUT,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightResponse> SpotlightPortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        return await SendApiRequestAndPollAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightResponse, PortfolioSpotlightAnalysis>(
            endpoint,
            NoData,
            MIN_TIMEOUT,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc />
    public async Task<AnalyzeTickerResponse> AnalyzeTickerAsync(string symbol, string? portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_FINHUB_AI_ANALYZE_TICKER.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(portfolioId))
        {
            var queryParams = new Dictionary<string, string?> { { "pid", portfolioId } };
            endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
        }
        return await SendApiRequestAndPollAsync<AnalyzeTickerAsyncResponse, AnalyzeTickerResponse, TickerAnalysis>(
            endpoint,
            NoData,
            MIN_TIMEOUT,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }
}
