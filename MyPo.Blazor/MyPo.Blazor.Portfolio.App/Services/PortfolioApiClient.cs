using MyPo.Blazor.App.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public class PortfolioApiClient : ApiClient, IPortfolioApiClient
{
	public PortfolioApiClient(HttpClient httpClient) : base(httpClient) { }

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<MarketDefResp>>> GetMarketsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MARKETS,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<MarketDefResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<PortfolioRecResp>>> GetMyPortfolioAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<PortfolioRecResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioRecResp>> CreatePortfolioAsync(CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Post, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioRecResp>> UpdateMyPortfolioAsync(string id, CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Put, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioRecResp>> DeleteMyPortfolioAsync(string id, string authToken, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Delete, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}
}
