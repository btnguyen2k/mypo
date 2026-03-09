using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	private static DateTimeOffset PrevWorkingDay(DateTimeOffset date)
	{
		var prevDay = date.AddDays(-1);
		while (prevDay.DayOfWeek == DayOfWeek.Saturday || prevDay.DayOfWeek == DayOfWeek.Sunday)
		{
			prevDay = prevDay.AddDays(-1);
		}
		return prevDay;
	}

	/// <summary>
	/// Gets incoming market events
	/// </summary>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_MARKET_EVENTS)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<MarketEventResp>>>> GetUpcomingMarketEvents()
	{
		var now = DateTimeOffset.UtcNow;
		var currentDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

		var startDateDiv = PrevWorkingDay(PrevWorkingDay(currentDate));
		var endDateDiv = currentDate.AddDays(6);
		var eventsDividend = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateDiv, endDateDiv,
			[MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION]);

		var startDateEarnings = currentDate;
		var endDateEarnings = currentDate.AddDays(3);
		var eventsEarnings = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateEarnings, endDateEarnings,
			[MarketEventEntity.EVENT_EARNINGS]);

		var startDateListing = currentDate.AddDays(-21);
		var endDateListing = currentDate.AddDays(14);
		var eventsListing = await PortfolioRepository.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateListing, endDateListing,
			[MarketEventEntity.EVENT_LISTING]);

		var result = eventsDividend.Select(x => MarketEventResp.BuildFrom(x)).ToList();
		result.AddRange(eventsEarnings.Select(x => MarketEventResp.BuildFrom(x)));
		result.AddRange(eventsListing.Select(x => MarketEventResp.BuildFrom(x)));
		return ResponseOk(result.OrderBy(x => x.EventTime));
	}
}
