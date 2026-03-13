using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc/>
	public async Task<ApiResp> SaveMyPreferencesMarketAlertAsync(SaveMyPrefMarketAlertReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IPortfolioApiClient.API_MY_PREFERENCES_MARKET_ALERT,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync(httpResult, cancellationToken);
	}
}
