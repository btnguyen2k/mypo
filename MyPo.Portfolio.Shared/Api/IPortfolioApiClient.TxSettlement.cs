using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS = "/api/my_portfolio/{id}/settlements";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID = "/api/my_portfolio/{id}/settlement/{txid}";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL = "/api/my_portfolio/{id}/pnl";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS"/> to get transactions for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<TxSettlementResp>>> GetMyPortfolioTxSettlementsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS"/> to create a new transaction for a given portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TxSettlementResp>> CreateMyPortfolioTxSettlementAsync(CreateOrUpdateTxSettlementReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID"/> to update an existing transaction from a given portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TxSettlementResp>> UpdateMyPortfolioTxSettlementAsync(CreateOrUpdateTxSettlementReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID"/> to delete an existing transaction from a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="txid"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TxSettlementResp>> DeleteMyPortfolioTxSettlementAsync(string portfolioId, string txid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL"/> to get PnL summary for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PnlSummaryResp>> GetMyPortfolioPnlSummaryAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
