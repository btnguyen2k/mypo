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
	public async ValueTask<ActionResult<ApiResp<IEnumerable<MarketEventResp>>>> GetUpcomingMarketEvents()
	{
		var now = DateTimeOffset.UtcNow;
		var currentDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
		var next7Days = currentDate.AddDays(7);
		var next14Days = currentDate.AddDays(14);
		var prev21Days = currentDate.AddDays(-21);
		var eventsDividend = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			currentDate, next7Days,
			[MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION]);
		var eventsEarnings = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			currentDate, next14Days,
			[MarketEventEntity.EVENT_EARNINGS]);
		var eventsListing = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			prev21Days, next14Days,
			[MarketEventEntity.EVENT_LISTING]);
		var result = eventsDividend.Select(x => MarketEventResp.BuildFrom(x)).ToList();
		result.AddRange(eventsEarnings.Select(x => MarketEventResp.BuildFrom(x)));
		result.AddRange(eventsListing.Select(x => MarketEventResp.BuildFrom(x)));
		return ResponseOk(result.OrderBy(x => x.EventTime));
	}
}
