using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc/>
	public async Task<ApiResp<IDictionary<string, StockQuote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var queryParams = new Dictionary<string, string>{ { "symbols", symbols } };
		var endpoint = QueryHelpers.AddQueryString(IPortfolioApiClient.API_FINHUB_STOCKS_GET_QUOTES, queryParams);
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
		var endpoint = IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_OVERVIEW
			.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
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
		var endpoint = IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_INFO
			.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<SymbolInfo>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<HistoryPoint>> GetStockQuoteAtDateAsync(string symbol, DateTime date, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE
			.Replace("{symbol}", symbol, StringComparison.OrdinalIgnoreCase)
			.Replace("{date}", date.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<HistoryPoint>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<PortfolioAnalysis>> AnalyzePortfolioPlanAsync(string planId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO
			.Replace("{id}", planId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PortfolioAnalysis>(httpResult, cancellationToken);
	}
}
