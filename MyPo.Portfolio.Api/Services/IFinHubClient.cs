using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public interface IFinHubClient
{
	public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW = "/stocks/{symbol}/overview";
	public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO = "/stocks/{symbol}/info";
	public const string API_FINHUB_ENDPOINT_STOCK_QUOTES = "/stocks/quotes";

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
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_QUOTES"/> to get stock quotes for the given symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<IDictionary<string, StockQuote>>> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/*----------------------------------------------------------------------*/

	public const string API_FINHUB_ENDPOINT_AI_EVENT_EARNINGS = "/ai/event/earnings";
	public const string API_FINHUB_ENDPOINT_AI_EVENT_DIVIDENDS = "/ai/event/dividends";

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_AI_EVENT_EARNINGS"/> to get upcoming earnings announcements for the given country and index.
	/// </summary>
	/// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
	/// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<IncomingEarningsEvent>>> GetIncomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_AI_EVENT_DIVIDENDS"/> to get upcoming dividend/distribution announcements for the given country and index.
	/// </summary>
	/// <param name="country">2-leter country code to filter events (e.g. AU, US, VN, etc)</param>
	/// <param name="index">Optional stock index to filter events by (e.g., NASDAQ 100, S&P/ASX 200, etc).</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<IncomingDividendEvent>>> GetIncomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
