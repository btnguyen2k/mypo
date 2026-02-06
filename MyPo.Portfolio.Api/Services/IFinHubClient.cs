using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public interface IFinHubClient
{
	// public const string API_FINHUB_ENDPOINT_SYMBOL_INFO = "/stocks/{symbol}/info";
	public const string API_FINHUB_ENDPOINT_STOCK_QUOTES = "/stocks/quotes";

	// /// <summary>
	// /// Calls the API <see cref="API_FINHUB_ENDPOINT_SYMBOL_INFO"/> to get information about a symbol.
	// /// </summary>
	// /// <param name="symbol">The symbol to get information about.</param>
	// /// <param name="baseUrl">The base URL of the API, optional.</param>
	// /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	// /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	// /// <returns></returns>
	// public Task<ApiResp<SymbolInfo>> GetSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_QUOTES"/> to get stock quotes for the given symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols.</param>
	/// <param name="baseUrl">The base URL of the API, optional.</param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
	/// <returns></returns>
	public Task<ApiResp<IDictionary<string, StockQuote>>> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
