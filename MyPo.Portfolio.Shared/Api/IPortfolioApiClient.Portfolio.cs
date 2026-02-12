using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO = "/api/my_portfolio";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID = "/api/my_portfolio/{id}";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO"/> to get current user's portfolio records.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<PortfolioResp>>> GetMyPortfoliosAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO"/> to create a new portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioResp>> CreatePortfolioAsync(CreateOrUpdatePortfolioReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID"/> to update an existing portfolio.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioResp>> UpdateMyPortfolioAsync(string id, CreateOrUpdatePortfolioReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID"/> to delete an existing portfolio.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioResp>> DeleteMyPortfolioAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
