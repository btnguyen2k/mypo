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
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_PORTFOLIO;
        return await SendApiRequestAndPollAsync<AnalyzePortfolioAsyncResponse, AnalyzePortfolioResponse, IPortfolioAnalysisResult>(
            HttpMethod.Get, endpointStart, NoData,
            HttpMethod.Get, endpointPoll,
            MIN_TIMEOUT,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> StartAnalyzePortfolioPlanAsync(
        string planId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        return await StartApiRequestAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(
            HttpMethod.Get, endpointStart, NoData,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> PollAnalyzePortfolioPlanAsync(
        string taskId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_PORTFOLIO;
        return await PollApiResultAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(
            HttpMethod.Get, endpointPoll, taskId,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightResponse> SpotlightPortfolioPlanAsync(
        string planId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_SPOTLIGHT_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_SPOTLIGHT_PORTFOLIO;
        return await SendApiRequestAndPollAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Get, endpointStart, NoData,
            HttpMethod.Get, endpointPoll,
            MIN_TIMEOUT,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> StartSpotlightPortfolioPlanAsync(
        string planId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_SPOTLIGHT_PORTFOLIO.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
        return await StartApiRequestAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Get, endpointStart, NoData,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> PollSpotlightPortfolioPlanAsync(
        string taskId,
        string authToken,
        string? baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_SPOTLIGHT_PORTFOLIO;
        return await PollApiResultAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Get, endpointPoll, taskId,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc />
    public async Task<AnalyzeTickerResponse> AnalyzeTickerAsync(
        string symbol,
        string? portfolioId,
        string authToken, string?
        baseUrl = default,
        HttpClient? requestHttpClient = default,
        CancellationToken cancellationToken = default)
    {
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_TICKER.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(portfolioId))
        {
            var queryParams = new Dictionary<string, string?> { { "pid", portfolioId } };
            endpointStart = QueryHelpers.AddQueryString(endpointStart, queryParams);
        }
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_TICKER;
        return await SendApiRequestAndPollAsync<AnalyzeTickerAsyncResponse, AnalyzeTickerResponse, TickerAnalysis>(
            HttpMethod.Get, endpointStart, NoData,
            HttpMethod.Get, endpointPoll,
            MIN_TIMEOUT,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> StartAnalyzeTickerAsync(string symbol, string? portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_TICKER.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(portfolioId))
        {
            var queryParams = new Dictionary<string, string?> { { "pid", portfolioId } };
            endpointStart = QueryHelpers.AddQueryString(endpointStart, queryParams);
        }
        return await StartApiRequestAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(
            HttpMethod.Get, endpointStart, NoData,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> PollAnalyzeTickerAsync(string taskId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_TICKER;
        return await PollApiResultAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(
            HttpMethod.Get, endpointPoll, taskId,
            authToken,
            baseUrl,
            requestHttpClient,
            cancellationToken);
    }
}
