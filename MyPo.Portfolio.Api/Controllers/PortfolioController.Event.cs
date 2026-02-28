using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	/// <summary>
	/// Gets incoming market events
	/// </summary>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_MARKET_EVENTS)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<MarketEventResp>>>> GetIncomingMarketEvents()
	{
		var result = new List<MarketEventResp>();
		var events = await PortfolioRepository.GetIncomingMarketEventsAsync(MarketEventEntity.NON_MARKET);
		foreach (var e in events)
		{
			result.Add(MarketEventResp.BuildFrom(e));
		}
		return ResponseOk(result);
	}
}
