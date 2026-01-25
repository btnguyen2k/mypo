using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS = "/api/my_portfolio/{id}/roi_recs";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID = "/api/my_portfolio/{id}/roi_rec/{rrid}";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL = "/api/my_portfolio/{id}/pnl";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS"/> to create a new ROI record for a given portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<RoiRecResp>> CreateMyPortfolioRoiRecAsync(CreateOrUpdateRoiRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID"/> to update an existing ROI record from a given portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<RoiRecResp>> UpdateMyPortfolioRoiRecAsync(CreateOrUpdateRoiRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID"/> to delete an existing ROI record from a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="rid"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<RoiRecResp>> DeleteMyPortfolioRoiRecAsync(string portfolioId, string rid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS"/> to get ROI records for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<RoiRecResp>>> GetMyPortfolioRoiRecsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL"/> to get PnL summary for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PnlSummaryResp>> GetMyPortfolioPnlSummaryAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
