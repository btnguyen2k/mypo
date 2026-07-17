using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
    public const string API_DEBUG = "/api/debug";
    public Task<ApiResp<string[]>> DebugAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_PORTFOLIO_ENDPOINT_MARKETS = "/api/markets";

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MARKETS"/> to get markets metadata.
    /// </summary>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<MarketDefResp>>> GetMarketsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
