using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public interface IFinHubClient
{
	public const string API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT = "/ai/analyze_dividend_event";
	public const string API_FINHUB_AI_ANALYZE_PORTFOLIO = "/ai/analyze_portfolio";

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

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_AI_ANALYZE_PORTFOLIO"/> to analyze a portfolio.
	/// </summary>
	/// <param name="holdings">Current holdings, format {ticker:allocation}</param>
	/// <param name="country"></param>
	/// <param name="investorTheme"></param>
	/// <param name="baseUrl"></param>
	/// <param name="httpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioAsync(IDictionary<string, decimal> holdings, string country, string? investorTheme, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

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

	public const string API_FINHUB_ENDPOINT_STOCK_QUOTES = "/stocks/quotes";
	public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW = "/stocks/{symbol}/overview";
	public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO = "/stocks/{symbol}/info";
	public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT = "/stocks/{symbol}/quote_at/{date}";

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_QUOTES"/> to get stock quotes for the given symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<IDictionary<string, StockQuote>>> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW"/> to get overview information about a stock symbol.
	/// </summary>
	/// <param name="symbol">The symbol to get information about.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO"/> to get detailed information about a stock symbol.
	/// </summary>
	/// <param name="symbol">The symbol to get information about.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT"/> to get stock quote for the given symbol at the specified date.
	/// </summary>
	/// <param name="symbol">The symbol to get quote for.</param>
	/// <param name="date">The date to get quote at, in the format YYYY-MM-DD.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, string date, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

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
