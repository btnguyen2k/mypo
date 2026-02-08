using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseClient, IFinHubClient
{
	public FinHubClient(ILogger<FinHubClient> logger, HttpClient httpClient, string baseUrl = "") : base(logger, httpClient, baseUrl) { }

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
}
