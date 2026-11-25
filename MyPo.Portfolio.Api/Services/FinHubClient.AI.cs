using Finhub.Client;
using FinHub.Client.Models.Dividends;
using FinHub.Client.Models.Portfolios;
using FinHub.Client.Models.Tickers;
using FinHub.Client.Schemas.DividendAnalysis;
using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Schemas.PortfolioConstruction;
using FinHub.Client.Schemas.PortfolioSpotlight;
using FinHub.Client.Schemas.TickerAnalysis;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient
{
    /// <inheritdoc/>
    public async Task<AnalyzeTickerResponse> AnalyzeTickerAsync(AnalyzeTickerRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER_ASYNC_START}";
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER_ASYNC_POLL}";
        return await SendApiRequestAndPollAsync<AnalyzeTickerAsyncResponse, AnalyzeTickerResponse, TickerAnalysis>(
            HttpMethod.Post, endpointStart, req,
            HttpMethod.Post, endpointPoll,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> StartAnalyzeTickerAsync(AnalyzeTickerRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER_ASYNC_START}";
        return await StartApiRequestAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(
            HttpMethod.Post, endpointStart, req,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> PollAnalyzeTickerAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER_ASYNC_POLL}";
        return await PollApiResultAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(
            HttpMethod.Post, endpointPoll, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventResponse> AnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT_ASYNC_START}";
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT_ASYNC_POLL}";
        return await SendApiRequestAndPollAsync<AnalyzeDividendEventAsyncResponse, AnalyzeDividendEventResponse, DividendEventAnalysis>(
            HttpMethod.Post, endpointStart, req,
            HttpMethod.Post, endpointPoll,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventAsyncResponse> StartAnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT_ASYNC_START}";
        return await StartApiRequestAsync<AnalyzeDividendEventAsyncResponse, DividendEventAnalysis>(
            HttpMethod.Post, endpointStart, req,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventAsyncResponse> PollAnalyzeDividendEventAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT_ASYNC_POLL}";
        return await PollApiResultAsync<AnalyzeDividendEventAsyncResponse, DividendEventAnalysis>(
            HttpMethod.Post, endpointPoll, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<BuildPortfolioResponse> BuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO_ASYNC_START}";
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO_ASYNC_POLL}";
        return await SendApiRequestAndPollAsync<BuildPortfolioAsyncResponse, BuildPortfolioResponse, PortfolioConstruction>(
            HttpMethod.Post, endpointStart, req,
            HttpMethod.Post, endpointPoll,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BuildPortfolioAsyncResponse> StartBuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO_ASYNC_START}";
        return await StartApiRequestAsync<BuildPortfolioAsyncResponse, PortfolioConstruction>(
            HttpMethod.Post, endpointStart, req,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BuildPortfolioAsyncResponse> PollBuildPortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO_ASYNC_POLL}";
        return await PollApiResultAsync<BuildPortfolioAsyncResponse, PortfolioConstruction>(
            HttpMethod.Post, endpointPoll, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioResponse> AnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO_ASYNC_START}";
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO_ASYNC_POLL}";
        return await SendApiRequestAndPollAsync<AnalyzePortfolioAsyncResponse, AnalyzePortfolioResponse, IPortfolioAnalysisResult>(
            HttpMethod.Post, endpointStart, req,
            HttpMethod.Post, endpointPoll,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> StartAnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO_ASYNC_START}";
        return await StartApiRequestAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(
            HttpMethod.Post, endpointStart, req,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> PollAnalyzePortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO_ASYNC_POLL}";
        return await PollApiResultAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(
            HttpMethod.Post, endpointPoll, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightResponse> SpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO_ASYNC_START}";
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO_ASYNC_POLL}";
        return await SendApiRequestAndPollAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Post, endpointStart, req,
            HttpMethod.Post, endpointPoll,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> StartSpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointStart = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO_ASYNC_START}";
        return await StartApiRequestAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Post, endpointStart, req,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> PollSpotlightPortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpointPoll = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO_ASYNC_POLL}";
        return await PollApiResultAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(
            HttpMethod.Post, endpointPoll, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }
}
