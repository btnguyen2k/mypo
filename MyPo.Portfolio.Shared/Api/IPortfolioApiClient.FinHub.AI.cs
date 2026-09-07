using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Schemas.PortfolioSpotlight;
using FinHub.Client.Schemas.TickerAnalysis;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
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
    public Task<AnalyzePortfolioResponse> AnalyzePortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

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
    public Task<PortfolioSpotlightResponse> SpotlightPortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_AI_ANALYZE_TICKER = "/api/finhub/ai/analyze_ticker/{symbol}";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_TICKER"/> to analyze a given ticker.
    /// </summary>
    /// <param name="symbol">The ticker symbol to analyze</param>
    /// <param name="portfolioId">The optional portfolio ID associated with the ticker</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AnalyzeTickerResponse> AnalyzeTickerAsync(string symbol, string? portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
