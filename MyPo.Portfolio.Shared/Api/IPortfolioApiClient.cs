using Finance.Net.Models.Yahoo;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient : IApiClient
{
	public const string API_PORTFOLIO_ENDPOINT_MARKETS = "/api/markets";

	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO = "/api/my_portfolio";
	public const string API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID = "/api/my_portfolio/{id}";

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MARKETS"/> to get markets metadata.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<MarketDefResp>>> GetMarketsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO"/> to get current user's portfolio records.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<PortfolioRecResp>>> GetMyPortfolioAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO"/> to create a new portfolio.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioRecResp>> CreatePortfolioAsync(CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID"/> to update an existing portfolio.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioRecResp>> UpdateMyPortfolioAsync(string id, CreateOrUpdatePortfolioRecReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID"/> to delete an existing portfolio.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<PortfolioRecResp>> DeleteMyPortfolioAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/*----------------------------------------------------------------------*/

	public const string API_STOCKS_GET_QUOTES = "/api/stocks/quotes";

	/// <summary>
	/// Calls the API <see cref="API_STOCK_GET_QUOTES"/> to get stock quotes for the given symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols, where each symbol follows the format stock-code:market-id.</param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IDictionary<string, Quote>>> GetStocksQuotesAsync(string symbols, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
