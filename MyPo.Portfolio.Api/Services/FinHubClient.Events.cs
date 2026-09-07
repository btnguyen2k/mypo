using Finhub.Client;
using FinHub.Client.Schemas.Events;
using FinHub.Client.Schemas.NewListings;
using Microsoft.AspNetCore.WebUtilities;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient
{
    /// <inheritdoc/>
    public async Task<GetUpcomingDividendsResponse> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendApiRequestAndPoll<GetUpcomingDividendsResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetUpcomingEarningsResponse> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendApiRequestAndPoll<GetUpcomingEarningsResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetNewListingsResponse> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS}_async", queryParams);
        async Task<HttpResponseMessage> buildAndSendTaskRequest() => await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Get, baseUrl, endpoint,
            NoAuth,
            NoData,
            cancellationToken
        );
        async Task<HttpResponseMessage> buildAndSendPollRequest(string taskId)
        {
            var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
            var endpointPoll = QueryHelpers.AddQueryString(endpoint, queryParams);
            return await BuildAndSendRequestAsync(
                httpClient,
                HttpMethod.Get, baseUrl, endpointPoll,
                NoAuth,
                NoData,
                cancellationToken
            );
        }
        return await SendApiRequestAndPoll<GetNewListingsResponse>(buildAndSendTaskRequest, buildAndSendPollRequest, MIN_TIMEOUT, cancellationToken);
    }
}
