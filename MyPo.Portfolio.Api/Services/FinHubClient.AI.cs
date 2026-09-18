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
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER}_async";
        return await SendApiRequestAndPollAsync<AnalyzeTickerAsyncResponse, AnalyzeTickerResponse, TickerAnalysis>(
            endpoint,
            req,
            MIN_TIMEOUT,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> StartAnalyzeTickerAsync(AnalyzeTickerRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER}_async";
        return await StartApiRequestAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(endpoint, req, baseUrl, httpClient, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeTickerAsyncResponse> PollAnalyzeTickerAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_TICKER}_async";
        return await PollApiResultAsync<AnalyzeTickerAsyncResponse, TickerAnalysis>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventResponse> AnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT}_async";
        return await SendApiRequestAndPollAsync<AnalyzeDividendEventAsyncResponse, AnalyzeDividendEventResponse, DividendEventAnalysis>(
            endpoint,
            req,
            MIN_TIMEOUT,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventAsyncResponse> StartAnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT}_async";
        return await StartApiRequestAsync<AnalyzeDividendEventAsyncResponse, DividendEventAnalysis>(endpoint, req, baseUrl, httpClient, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventAsyncResponse> PollAnalyzeDividendEventAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT}_async";
        return await PollApiResultAsync<AnalyzeDividendEventAsyncResponse, DividendEventAnalysis>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<BuildPortfolioResponse> BuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO}_async";
        return await SendApiRequestAndPollAsync<BuildPortfolioAsyncResponse, BuildPortfolioResponse, PortfolioConstruction>(
            endpoint,
            req,
            MIN_TIMEOUT,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BuildPortfolioAsyncResponse> StartBuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO}_async";
        return await StartApiRequestAsync<BuildPortfolioAsyncResponse, PortfolioConstruction>(endpoint, req, baseUrl, httpClient, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BuildPortfolioAsyncResponse> PollBuildPortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_BUILD_PORTFOLIO}_async";
        return await PollApiResultAsync<BuildPortfolioAsyncResponse, PortfolioConstruction>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioResponse> AnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO}_async";
        return await SendApiRequestAndPollAsync<AnalyzePortfolioAsyncResponse, AnalyzePortfolioResponse, IPortfolioAnalysisResult>(
            endpoint,
            req,
            MIN_TIMEOUT,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> StartAnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO}_async";
        return await StartApiRequestAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(endpoint, req, baseUrl, httpClient, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioAsyncResponse> PollAnalyzePortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_PORTFOLIO}_async";
        return await PollApiResultAsync<AnalyzePortfolioAsyncResponse, IPortfolioAnalysisResult>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightResponse> SpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO}_async";
        return await SendApiRequestAndPollAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightResponse, PortfolioSpotlightAnalysis>(
            endpoint,
            req,
            MIN_TIMEOUT,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> StartSpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO}_async";
        return await StartApiRequestAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(endpoint, req, baseUrl, httpClient, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightAsyncResponse> PollSpotlightPortfolioAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO}_async";
        return await PollApiResultAsync<PortfolioSpotlightAsyncResponse, PortfolioSpotlightAnalysis>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
    }
}
