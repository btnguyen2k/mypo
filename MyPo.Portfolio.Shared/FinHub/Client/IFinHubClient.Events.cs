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
    /// <returns></returns>
    public Task<GetUpcomingDividendsResponse> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS = "/events/upcoming_earnings";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS"/> to get upcoming earnings announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetUpcomingEarningsResponse> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

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
}
