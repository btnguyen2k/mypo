using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseClient, IFinHubClient
{
	public FinHubClient(ILogger<FinHubClient> logger, HttpClient httpClient, string baseUrl = "") : base(logger, httpClient, baseUrl) { }

	/// <inheritdoc/>
	public async Task<ApiResp<SymbolOverview>> GetStockSymbolOverviewAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_OVERVIEW.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, endpoint,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolOverview>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<SymbolInfo>> GetStockSymbolInfoAsync(string symbol, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IFinHubClient.API_FINHUB_ENDPOINT_STOCK_SYMBOL_INFO.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, endpoint,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolInfo>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<IDictionary<string, StockQuote>>> GetStockQuotesAsync(string symbols, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_STOCK_QUOTES, new Dictionary<string, string?> { { "symbols", symbols } });
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, endpoint,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IDictionary<string, StockQuote>>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<IncomingEarningsEvent>>> GetIncomingEarningsAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var queryParams = new Dictionary<string, string?> { { "country", country } };
		if (!string.IsNullOrWhiteSpace(index))
		{
			queryParams["index"] = index;
		}
		var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_AI_EVENT_EARNINGS, queryParams);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, endpoint,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<IncomingEarningsEvent>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<IncomingDividendEvent>>> GetIncomingDividendAnnouncementsAsync(string country, string? index = default, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		var queryParams = new Dictionary<string, string?> { { "country", country } };
		if (!string.IsNullOrWhiteSpace(index))
		{
			queryParams["index"] = index;
		}
		var endpoint = QueryHelpers.AddQueryString(IFinHubClient.API_FINHUB_ENDPOINT_AI_EVENT_DIVIDENDS, queryParams);
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, endpoint,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<IncomingDividendEvent>>(httpResult, cancellationToken);
	}
}
