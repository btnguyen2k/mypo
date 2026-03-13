using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
	public const string API_STOCKS_GET_QUOTES = "/api/stocks/quotes";
	public const string API_STOCKS_SYMBOL_INFO = "/api/stocks/{symbol}/info";
	public const string API_STOCKS_SYMBOL_OVERVIEW = "/api/stocks/{symbol}/overview";
	public const string API_STOCKS_SYMBOL_QUOTE_AT_DATE = "/api/stocks/{symbol}/quote/{date}";

	/// <summary>
	/// Calls the API <see cref="API_STOCK_GET_QUOTES"/> to get stock quotes for the given symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols, where each symbol follows the format stock-code:market-id.</param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IDictionary<string, StockQuote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_STOCKS_SYMBOL_OVERVIEW"/> to get stock symbol overview info for the given symbol.
	/// </summary>
	/// <param name="symbol">Symbol follows the format stock-code:market-id.</param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_STOCKS_SYMBOL_INFO"/> to get stock symbol detailed info for the given symbol.
	/// </summary>
	/// <param name="symbol">Symbol follows the format stock-code:market-id.</param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_STOCKS_SYMBOL_QUOTE_AT_DATE"/> to get stock quote for the given symbol at the given date.
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="date"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, DateTime date, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
