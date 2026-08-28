using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace Finhub.Client;

public partial interface IFinHubClient
{
    public const string API_FINHUB_AI_ANALYZE_TICKER = "/ai/analyze_ticker";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_TICKER"/> to analyze a ticker symbol.
    /// </summary>
    /// <param name="req">The analysis request.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<ApiResp<TickerAnalysis>> AnalyzeTickerAsync(AnalyzeTickerReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT = "/ai/analyze_dividend_event";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT"/> to analyze a dividend event.
    /// </summary>
    /// <param name="symbol">The stock symbol of the dividend event.</param>
    /// <param name="exDate">The ex-dividend date of the event, in the format YYYY-MM-DD.</param>
    /// <param name="divAmount">The dividend amount of the event.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<ApiResp<DividendEventAnalysis>> AnalyzeDividendEventAsync(string symbol, string exDate, decimal divAmount, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_BUILD_PORTFOLIO = "/ai/build_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_BUILD_PORTFOLIO"/> to build a new portfolio.
    /// </summary>
    /// <param name="req">The portfolio building request</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<PortfolioAnalysis>> BuildPortfolioAsync(BuildPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_ANALYZE_PORTFOLIO = "/ai/analyze_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_PORTFOLIO"/> to analyze a an existing portfolio.
    /// </summary>
    /// <param name="req">The portfolio analyze request</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioAsync(AnalyzePortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_SPOTLIGHT_PORTFOLIO = "/ai/spotlight_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_SPOTLIGHT_PORTFOLIO"/> to analyze a an existing portfolio for immediate risks and actions.
    /// </summary>
    /// <param name="req">The portfolio analyze request</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<PortfolioAnalysis>> SpotlightPortfolioAsync(SpotLightPortfolioReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS = "/events/upcoming_dividends";
    public const string API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS = "/events/upcoming_earnings";
    public const string API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS = "/events/new_listings";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_DIVIDENDS"/> to get upcoming dividend/distribution announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<UpcomingDividendEvent>>> GetUpcomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_UPCOMING_EARNINGS"/> to get upcoming earnings announcements for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<UpcomingEarningsEvent>>> GetUpcomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_EVENTS_NEW_LISTINGS"/> to get new listings for the given country and index.
    /// </summary>
    /// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
    /// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<ListingEvent>>> GetNewListingAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/


    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_ENDPOINT_TOZ_GOLD_QUOTE = "/toz/gold/quote";
    public const string API_FINHUB_ENDPOINT_TOZ_GOLD_HISTORY = "/toz/gold/history";
    public const string API_FINHUB_ENDPOINT_TOZ_SILVER_QUOTE = "/toz/silver/quote";
    public const string API_FINHUB_ENDPOINT_TOZ_SILVER_HISTORY = "/toz/silver/history";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_GOLD_QUOTE"/> to get the latest gold price quote.
    /// </summary>
    /// <param name="currency">The currency to get the gold price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<StockQuote>> GetGoldQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_GOLD_HISTORY"/> to get historical gold price data for the past number of days.
    /// </summary>
    /// <param name="currency">The currency to get the gold price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="days">The number of past days to get historical data for, optional, default is 30.</param>
    /// <param name="baseUrl"></param>
    /// param name="httpClient"></param>
    /// param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<HistoryPoint>>> GetGoldPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_SILVER_QUOTE"/> to get the latest silver price quote.
    /// </summary>
    /// <param name="currency">The currency to get the silver price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<StockQuote>> GetSilverQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_SILVER_HISTORY"/> to get historical silver price data for the past number of days.
    /// </summary>
    /// <param name="currency">The currency to get the silver price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="days">The number of past days to get historical data for, optional, default is 30.</param>
    /// <param name="baseUrl"></param>
    /// param name="httpClient"></param>
    /// param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<HistoryPoint>>> GetSilverPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
