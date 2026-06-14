using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
    public const string API_MY_PREFERENCES_MARKET_ALERT = "/api/my_preferences/market_alert";

    /// <summary>
    /// Calls the API <see cref="API_MY_PREFERENCES_MARKET_ALERT"/> to save my market alert preferences.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp> SaveMyPreferencesMarketAlertAsync(SaveMyPrefMarketAlertReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_MY_PREFERENCES_PORTFOLIO_PLAN = "/api/my_preferences/portfolio_plan";

    /// <summary>
    /// Calls the API <see cref="API_MY_PREFERENCES_PORTFOLIO_PLAN"/> to save my portfolio plan preferences.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp> SaveMyPreferencesPortfolioPlanAsync(SaveMyPrefPortfolioPlanReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
