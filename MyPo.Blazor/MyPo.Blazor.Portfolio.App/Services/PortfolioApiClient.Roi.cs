using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc />
	public async Task<ApiResp<RoiRecResp>> CreateMyPortfolioRoiRecAsync(CreateOrUpdateRoiRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoiRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<RoiRecResp>> UpdateMyPortfolioRoiRecAsync(CreateOrUpdateRoiRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID
			.Replace("{rrid}", req.Id, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoiRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<RoiRecResp>> DeleteMyPortfolioRoiRecAsync(string portfolioId, string rid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID
			.Replace("{rrid}", rid, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoiRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<IEnumerable<RoiRecResp>>> GetMyPortfolioRoiRecsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<RoiRecResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<PnlSummaryResp>> GetMyPortfolioPnlSummaryAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PnlSummaryResp>(httpResult, cancellationToken);
	}
}
