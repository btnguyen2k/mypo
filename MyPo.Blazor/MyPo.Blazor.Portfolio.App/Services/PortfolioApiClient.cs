using Microsoft.AspNetCore.WebUtilities;
using MyPo.Blazor.App.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
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
	public async Task<ApiResp<IDictionary<string, StockQuote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var queryParams = new Dictionary<string, string>{ { "symbols", symbols } };
		var endpoint = QueryHelpers.AddQueryString(IPortfolioApiClient.API_STOCKS_GET_QUOTES, queryParams);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IDictionary<string, StockQuote>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_STOCKS_SYMBOL_OVERVIEW.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolOverview>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_STOCKS_SYMBOL_INFO.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolInfo>(httpResult, cancellationToken);
	}
}
