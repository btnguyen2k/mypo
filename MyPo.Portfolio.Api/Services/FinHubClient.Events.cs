using Finhub.Client;
using FinHub.Client.Models.Events;
using FinHub.Client.Models.Listings;
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
        return await SendApiRequestAndPollAsync<GetUpcomingDividendsAsyncResponse, GetUpcomingDividendsResponse, IReadOnlyList<UpcomingDividendEvent>>(
            HttpMethod.Get, endpoint, NoData,
            HttpMethod.Get, endpoint,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetUpcomingDividendsAsyncResponse> StartUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS}_async", queryParams);
        return await StartApiRequestAsync<GetUpcomingDividendsAsyncResponse, IReadOnlyList<UpcomingDividendEvent>>(
            HttpMethod.Get, endpoint, NoData,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetUpcomingDividendsAsyncResponse> PollUpcomingDividendAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS}_async";
        return await PollApiResultAsync<GetUpcomingDividendsAsyncResponse, IReadOnlyList<UpcomingDividendEvent>>(
            HttpMethod.Get, endpoint, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<GetUpcomingEarningsResponse> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS}_async", queryParams);
        return await SendApiRequestAndPollAsync<GetUpcomingEarningsAsyncResponse, GetUpcomingEarningsResponse, IReadOnlyList<UpcomingEarningsEvent>>(
            HttpMethod.Get, endpoint, NoData,
            HttpMethod.Get, endpoint,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetUpcomingEarningsAsyncResponse> StartUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS}_async", queryParams);
        return await StartApiRequestAsync<GetUpcomingEarningsAsyncResponse, IReadOnlyList<UpcomingEarningsEvent>>(
            HttpMethod.Get, endpoint, NoData,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetUpcomingEarningsAsyncResponse> PollUpcomingEarningsAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS}_async";
        return await StartApiRequestAsync<GetUpcomingEarningsAsyncResponse, IReadOnlyList<UpcomingEarningsEvent>>(
            HttpMethod.Get, endpoint, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<GetNewListingsResponse> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS}_async", queryParams);
        return await SendApiRequestAndPollAsync<GetNewListingsAsyncResponse, GetNewListingsResponse, IReadOnlyList<ListingEvent>>(
            HttpMethod.Get, endpoint, NoData,
            HttpMethod.Get, endpoint,
            MIN_TIMEOUT,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetNewListingsAsyncResponse> StartNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "country", country } };
        if (!string.IsNullOrWhiteSpace(index))
        {
            queryParams["index"] = index;
        }
        var endpoint = QueryHelpers.AddQueryString($"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS}_async", queryParams);
        return await StartApiRequestAsync<GetNewListingsAsyncResponse, IReadOnlyList<ListingEvent>>(
            HttpMethod.Get, endpoint, NoData,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GetNewListingsAsyncResponse> PollNewListingAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{IFinHubClient.API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS}_async";
        return await StartApiRequestAsync<GetNewListingsAsyncResponse, IReadOnlyList<ListingEvent>>(
            HttpMethod.Get, endpoint, taskId,
            NoAuth,
            baseUrl,
            httpClient,
            cancellationToken);
    }
}
