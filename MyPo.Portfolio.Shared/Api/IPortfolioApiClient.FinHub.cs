using Finhub.Client;
using FinHub.Client.Schemas.Stocks;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
    public const string API_FINHUB_STOCKS_GET_QUOTES = $"/api/finhub/{IFinHubClient.API_FINHUB_ENDPOINT_STOCK_QUOTES}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_GET_QUOTES"/> to get stock quotes for the given symbols.
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetStockQuotesResponse> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_STOCKS_SYMBOL_OVERVIEW = $"/api/finhub/{IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_OVERVIEW"/> to get stock symbol overview info for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetSymbolOverviewResponse> GetStockSymbolOverviewAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_STOCKS_SYMBOL_INFO = $"/api/finhub/{IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_INFO"/> to get stock symbol detailed info for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetSymbolInfoResponse> GetStockSymbolInfoAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    // public const string API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE = $"/api/finhub/{IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_QUOTE_AT}";

    // /// <summary>
    // /// Calls the API <see cref="API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE"/> to get stock quote for the given symbol at the given date.
    // /// </summary>
    // /// <param name="symbol"></param>
    // /// <param name="date"></param>
    // /// <param name="authToken"></param>
    // /// <param name="baseUrl"></param>
    // /// <param name="requestHttpClient"></param>
    // /// <param name="cancellationToken"></param>
    // /// <returns></returns>
    // public Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, DateTime date, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

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
