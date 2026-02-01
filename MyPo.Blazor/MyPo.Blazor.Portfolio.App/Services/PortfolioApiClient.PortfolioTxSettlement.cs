using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
	/// <inheritdoc />
	public async Task<ApiResp<IEnumerable<TxSettlementResp>>> GetMyPortfolioTxSettlementsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<TxSettlementResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<TxSettlementResp>> CreateMyPortfolioTxSettlementAsync(CreateOrUpdateTxSettlementReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TxSettlementResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<TxSettlementResp>> UpdateMyPortfolioTxSettlementAsync(CreateOrUpdateTxSettlementReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID
			.Replace("{txid}", req.Id, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", req.PortfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, endpoint,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TxSettlementResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<TxSettlementResp>> DeleteMyPortfolioTxSettlementAsync(string portfolioId, string txid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID
			.Replace("{txid}", txid, StringComparison.OrdinalIgnoreCase)
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<TxSettlementResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ApiResp<PnlSummaryResp>> GetMyPortfolioPnlSummaryAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		var endpoint = IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL
			.Replace("{id}", portfolioId, StringComparison.OrdinalIgnoreCase);
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, endpoint,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<PnlSummaryResp>(httpResult, cancellationToken);
	}
}
