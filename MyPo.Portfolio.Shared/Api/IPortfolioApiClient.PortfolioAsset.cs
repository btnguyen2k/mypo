using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSETS = "/api/my_portfolio/{id}/assets";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSET_ID = "/api/my_portfolio/{id}/asset/{aid}";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSETS"/> to get assets for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<AssetResp>>> GetMyPortfolioAssetsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSET_ID"/> to update an existing asset.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<AssetResp>> UpdateMyPortfolioAssetAsync(CreateOrUpdateAssetReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
