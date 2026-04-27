using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS = "/api/my_portfolio_plans"; // GET my plans
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID = "/api/my_portfolio_plans/{id}"; // READ, UPDATE, DELETE my plans
	public const string API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLANS = "/api/portfolio_plans"; // CREATE

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS"/> to get current user's portfolio plan records.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<PortfolioPlanResp>>> GetMyPortfolioPlansAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID"/> to get current user's portfolio plan specified by the plan id.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioPlanResp>> GetMyPortfolioPlanByIdAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLANS"/> to create a new portfolio plan.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioPlanResp>> CreatePortfolioPlanAsync(CreateOrUpdatePortfolioPlanReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID"/> to update an existing portfolio plan.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioPlanResp>> UpdateMyPortfolioPlanAsync(string id, CreateOrUpdatePortfolioPlanReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID"/> to delete an existing portfolio plan.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioPlanResp>> DeleteMyPortfolioPlanAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
