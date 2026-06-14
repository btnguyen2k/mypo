using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
    public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_BUYS_SELLS = "/api/my_portfolio/{id}/buys_sells";
    public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_BUY_SELL_TX_ID = "/api/my_portfolio/{id}/buy_sell/{txid}";
    public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_BUY_SELL_TX_ID = "/api/my_portfolio/{id}/settle_buy_sell/{txid}";

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_BUYS_SELLS"/> to get transactions for a given portfolio.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<TxBuySellResp>>> GetMyPortfolioTxBuySellsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_BUYS_SELLS"/> to create a new transaction for a given portfolio.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<TxBuySellResp>> CreateMyPortfolioTxBuySellAsync(CreateOrUpdateTxBuySellReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_BUY_SELL_TX_ID"/> to update an existing transaction.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<TxBuySellResp>> UpdateMyPortfolioTxBuySellAsync(CreateOrUpdateTxBuySellReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_BUY_SELL_TX_ID"/> to delete an existing transaction.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="txid"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<TxBuySellResp>> DeleteMyPortfolioTxBuySellAsync(string portfolioId, string txid, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_BUY_SELL_TX_ID"/> to settle an existing transaction.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<TxBuySellResp>> SettleMyPortfolioTxAsync(CreateOrUpdateTxBuySellReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
