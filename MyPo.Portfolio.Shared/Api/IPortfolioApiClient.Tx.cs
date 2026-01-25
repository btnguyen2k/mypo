using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS = "/api/my_portfolio/{id}/transactions";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID = "/api/my_portfolio/{id}/tx/{txid}";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_TX_ID = "/api/my_portfolio/{id}/settle_tx/{txid}";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS"/> to create a new transaction for a given portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TransactionRecResp>> CreateMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID"/> to update an existing transaction.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TransactionRecResp>> UpdateMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID"/> to delete an existing transaction.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="txid"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TransactionRecResp>> DeleteMyPortfolioTxAsync(string portfolioId, string txid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_TX_ID"/> to settle an existing transaction.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<TransactionRecResp>> SettleMyPortfolioTxAsync(CreateOrUpdateTransactionRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS"/> to get transactions for a given portfolio.
	/// </summary>
	/// <param name="portfolioId"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<TransactionRecResp>>> GetMyPortfolioTransactionsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
