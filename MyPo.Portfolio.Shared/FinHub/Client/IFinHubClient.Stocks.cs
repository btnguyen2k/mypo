using FinHub.Client.Schemas.Stocks;

namespace Finhub.Client;

public partial interface IFinHubClient
{
    public const string API_FINHUB_ENDPOINT_STOCK_QUOTES = "/stocks/quotes";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_QUOTES"/> to get stock quotes for the given symbols.
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetStockQuotesResponse> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW = "/stocks/{symbol}/overview";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW"/> to get overview information about a stock symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetSymbolOverviewResponse> GetStockSymbolOverviewAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO = "/stocks/{symbol}/info";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO"/> to get detailed information about a stock symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetSymbolInfoResponse> GetStockSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT = "/stocks/{symbol}/quote_at/{date}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT"/> to get stock quote for the given symbol at the specified date.
    /// </summary>
    /// <param name="symbol">The symbol to get quote for, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="date">The date to get quote at, in the format YYYY-MM-DD.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetStockQuoteAtDateResponse> GetStockQuoteAtDateAsync(string symbol, string date, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_HISTORY = "/stocks/{symbol}/history";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_HISTORY"/> to get historical price data of a stock symbol for the past number of days.
    /// </summary>
    /// <param name="symbol">The symbol to get quote for, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="days">The number of past days to get historical data for, optional, default is 100.</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<GetStockHistoryResponse> GetStockQuoteHistoryAsync(string symbol, int? days = 100, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
