using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<AIVendor>>> GetAIVendorsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IPortfolioApiClient.API_AI_VENDORS,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<AIVendor>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<SymbolAnalysisResp>> AnalyzeSymbolAsync(SymbolAnalysisReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IPortfolioApiClient.API_AI_SYMBOL_ANALYSIS,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolAnalysisResp>(httpResult, cancellationToken);
	}
}
