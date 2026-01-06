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
}
