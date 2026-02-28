using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_MARKET_EVENTS = "/api/market_events";

	/// <summary>
	/// Calls the API <see cref="API_MARKET_EVENTS"/> to get incoming market events.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<MarketEventResp>>> GetIncomingMarketEventsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
