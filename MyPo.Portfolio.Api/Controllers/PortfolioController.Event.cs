using Ddth.Utilities.Tempus;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;
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
        var currentDate = DateTimeOffset.UtcNow.StartOfDay();

        var startDateDiv = currentDate.AddDays(-7);
        var endDateDiv = currentDate.AddDays(14);
        var eventsDividend = await PortfolioRepository.GetMarketEventsAsync(
            MarketEventEntity.NON_OWNER,
            startDateDiv, endDateDiv,
            [MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION]);

        var startDateEarnings = currentDate.PrevWeekDay();
        var endDateEarnings = currentDate.AddDays(5);
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

        var yieldsMap = eventsDividend
            .GroupBy(e => e.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Metadata?.Dividend?.DividendYield ?? 0m,
                StringComparer.OrdinalIgnoreCase);

        var result = eventsDividend.Select(e =>
        {
            var response = MarketEventResp.BuildFrom(e);
            response.AttentionLevel = MarketEventUtils.AttentionLevelForDividend(e, yieldsMap);
            return response;
        }).ToList();
        result.AddRange(eventsEarnings.Select(x => MarketEventResp.BuildFrom(x)));
        result.AddRange(eventsListing.Select(x => MarketEventResp.BuildFrom(x)));
        return ResponseOk(result.OrderBy(x => x.EventTime));
    }
}
