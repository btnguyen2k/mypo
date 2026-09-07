using FinHub.Client.Schemas.DividendAnalysis;
using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Schemas.PortfolioConstruction;
using FinHub.Client.Schemas.PortfolioSpotlight;
using FinHub.Client.Schemas.TickerAnalysis;

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
    public Task<AnalyzeTickerResponse> AnalyzeTickerAsync(AnalyzeTickerRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT = "/ai/analyze_dividend_event";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_DIVIDEND_EVENT"/> to analyze a dividend event.
    /// </summary>
    /// <param name="req">The analysis request.</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<AnalyzeDividendEventResponse> AnalyzeDividendEventAsync(AnalyzeDividendEventRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    /*----------------------------------------------------------------------*/

    public const string API_FINHUB_AI_BUILD_PORTFOLIO = "/ai/build_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_BUILD_PORTFOLIO"/> to build a new portfolio.
    /// </summary>
    /// <param name="req">The portfolio building request</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<BuildPortfolioResponse> BuildPortfolioAsync(BuildPortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_ANALYZE_PORTFOLIO = "/ai/analyze_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_ANALYZE_PORTFOLIO"/> to analyze a an existing portfolio.
    /// </summary>
    /// <param name="req">The portfolio analyze request</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<AnalyzePortfolioResponse> AnalyzePortfolioAsync(AnalyzePortfolioRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_AI_SPOTLIGHT_PORTFOLIO = "/ai/spotlight_portfolio";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_AI_SPOTLIGHT_PORTFOLIO"/> to analyze a an existing portfolio for immediate risks and actions.
    /// </summary>
    /// <param name="req">The portfolio analyze request</param>
    /// <param name="baseUrl">The base URL of the API, optional.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the API call, optional.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the API call, optional.</param>
    /// <returns></returns>
    public Task<PortfolioSpotlightResponse> SpotlightPortfolioAsync(PortfolioSpotlightRequest req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
