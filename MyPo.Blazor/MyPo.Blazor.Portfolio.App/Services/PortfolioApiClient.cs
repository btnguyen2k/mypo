using Finance.Net.Models.Yahoo;
using Microsoft.AspNetCore.WebUtilities;
using MyPo.Blazor.App.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient : ApiClient, IPortfolioApiClient
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
	public async Task<ApiResp<PortfolioRecResp>> CreatePortfolioAsync(CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioRecResp>> UpdateMyPortfolioAsync(string id, CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioRecResp>> DeleteMyPortfolioAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID.Replace("{id}", id, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioRecResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<IDictionary<string, Quote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		// var endpoint = IPortfolioApiClient.API_STOCKS_GET_QUOTES.Replace("{symbols}", symbols, StringComparison.OrdinalIgnoreCase);
		var queryParams = new Dictionary<string, string>{ { "symbols", symbols } };
		var endpoint = QueryHelpers.AddQueryString(IPortfolioApiClient.API_STOCKS_GET_QUOTES, queryParams);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IDictionary<string, Quote>>(httpResult, cancellationToken);
	}
}
