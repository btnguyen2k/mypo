using FinHub.Client.Schemas.Events;
using FinHub.Client.Schemas.NewListings;

namespace Finhub.Client;

public partial interface IFinHubClient
{
    public const string API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS = "/events/upcoming_dividends";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS"/> to get upcoming dividend/distribution announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the upcoming dividend/distribution announcements for the given country and index.</returns>
    public Task<GetUpcomingDividendsResponse> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS"/>_async to start an asynchronous fetching of upcoming dividend/distribution announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the task ID for the asynchronous fetching of upcoming dividend/distribution announcements.</returns>
    public Task<GetUpcomingDividendsAsyncResponse> StartUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS"/>_async to poll the result of the asynchronous fetching of upcoming dividend/distribution announcements for the given country and index.
    /// </summary>
    /// <param name="taskId">The task ID of the asynchronous fetching of upcoming dividend/distribution announcements.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the status and result of the asynchronous fetching of upcoming dividend/distribution announcements.</returns>
    public Task<GetUpcomingDividendsAsyncResponse> PollUpcomingDividendAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS = "/events/upcoming_earnings";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS"/> to get upcoming earnings announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing upcoming earnings announcements for the given country and index.</returns>
    public Task<GetUpcomingEarningsResponse> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS"/>_async to start fetching upcoming earnings announcements for the given country and index asynchronously.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the task ID for the asynchronous fetching of upcoming earnings announcements.</returns>
    public Task<GetUpcomingEarningsAsyncResponse> StartUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS"/>_async to poll the status of the asynchronous fetching of upcoming earnings announcements for the given country and index.
    /// </summary>
    /// <param name="taskId">The task ID of the asynchronous fetching of upcoming earnings announcements.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the status of the asynchronous fetching of upcoming earnings announcements.</returns>
    public Task<GetUpcomingEarningsAsyncResponse> PollUpcomingEarningsAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS = "/events/new_listings";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS"/> to get new listings for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetNewListingsResponse> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS"/>_async to start fetching new listings for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the status of the asynchronous fetching of new listings.</returns>
    public Task<GetNewListingsAsyncResponse> StartNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS"/>_async to poll the status of fetching new listings for the given country and index.
    /// </summary>
    /// <param name="taskId">The task ID of the asynchronous fetching of new listings.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns>The response containing the status of the asynchronous fetching of new listings.</returns>
    public Task<GetNewListingsAsyncResponse> PollNewListingAnnouncementsAsync(string taskId, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
