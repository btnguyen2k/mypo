using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
    public const string API_FINHUB_STOCKS_GET_QUOTES = "/api/finhub/stocks/quotes";
    public const string API_FINHUB_STOCKS_SYMBOL_INFO = "/api/finhub/stocks/{symbol}/info";
    public const string API_FINHUB_STOCKS_SYMBOL_OVERVIEW = "/api/finhub/stocks/{symbol}/overview";
    public const string API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE = "/api/finhub/stocks/{symbol}/quote/{date}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_GET_QUOTES"/> to get stock quotes for the given symbols.
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols, where each symbol follows the format stock-code:market-id.</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IDictionary<string, StockQuote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_OVERVIEW"/> to get stock symbol overview info for the given symbol.
    /// </summary>
    /// <param name="symbol">Symbol follows the format stock-code:market-id.</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_INFO"/> to get stock symbol detailed info for the given symbol.
    /// </summary>
    /// <param name="symbol">Symbol follows the format stock-code:market-id.</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE"/> to get stock quote for the given symbol at the given date.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="date"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, DateTime date, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_AI_ANALYZE_PORTFOLIO = "/api/finhub/ai/analyze_portfolio/{id}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_PORTFOLIO"/> to analyze the portfolio plan with AI and get the analysis result.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_SPOTLIGHT_PORTFOLIO = "/api/finhub/ai/spotlight_portfolio/{id}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_SPOTLIGHT_PORTFOLIO"/> to get the spotlight analysis for the portfolio plan with AI and get the analysis result.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<PortfolioAnalysis>> SpotlightPortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_AI_ANALYZE_TICKER = "/api/finhub/ai/analyze_ticker";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_TICKER"/> to analyze a given ticker.
    /// </summary>
    /// <param name="req">The analysis request</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<TickerAnalysis>> AnalyzeTickerAsync(TickerAnalysisReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
