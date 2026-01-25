using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<TransactionRecResp>>> GetMyPortfolioTransactionsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<TransactionRecResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<TransactionRecResp>> CreateMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TransactionRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<TransactionRecResp>> UpdateMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID
			.Replace("{txid}", req.Id, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TransactionRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<TransactionRecResp>> DeleteMyPortfolioTxAsync(string portfolioId, string txid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID
			.Replace("{txid}", txid, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TransactionRecResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<TransactionRecResp>> SettleMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_TX_ID
			.Replace("{txid}", req.Id, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TransactionRecResp>(httpResult, cancellationToken);
	}
}
