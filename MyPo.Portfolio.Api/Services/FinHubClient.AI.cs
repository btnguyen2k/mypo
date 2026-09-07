using Finhub.Client;
using FinHub.Client.Schemas.DividendAnalysis;
using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Schemas.PortfolioConstruction;
using FinHub.Client.Schemas.PortfolioSpotlight;
using FinHub.Client.Schemas.TickerAnalysis;
using Microsoft.AspNetCore.WebUtilities;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient
{
    /// <inheritdoc/>
    public async Task<AnalyzeTickerResponse> AnalyzeTickerAsync(AnalyzeTickerRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
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
        return await SendApiRequestAndPoll<AnalyzeTickerResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzeDividendEventResponse> AnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT}_async";
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
        return await SendApiRequestAndPoll<AnalyzeDividendEventResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<BuildPortfolioResponse> BuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
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
        return await SendApiRequestAndPoll<BuildPortfolioResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AnalyzePortfolioResponse> AnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
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
        return await SendApiRequestAndPoll<AnalyzePortfolioResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioSpotlightResponse> SpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
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
        return await SendApiRequestAndPoll<PortfolioSpotlightResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }
}
