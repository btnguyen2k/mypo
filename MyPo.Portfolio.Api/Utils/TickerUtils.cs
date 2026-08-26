using Ddth.Utilities.Tempus;
using Finhub.Client;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Utils;

public static class TickerUtils
{
    private static readonly Dictionary<string, StockQuote> EmptyDict = [];

    /// <summary>
    /// Fetches the latest stock quotes for the given tickers in batches to avoid hitting API limits.
    /// </summary>
    /// <param name="tickers"></param>
    /// <param name="finHubClient"></param>
    /// <param name="preBatchFetchAction">An optional action to perform before fetching each batch of quotes. The list of tickers in the current batch will be passed as a parameter.</param>
    /// <param name="postBatchFetchAction">An optional action to perform after fetching each batch of quotes. The API response will be passed as a parameter.</param>
    /// <param name="onBatchFetchException">An optional action to perform if an exception occurs while fetching a batch of quotes. The exception will be passed as a parameter.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<IDictionary<string, StockQuote>> FetchQuotesForTickersAsync(
        IEnumerable<string> tickers,
        IFinHubClient finHubClient,
        Action<List<string>>? preBatchFetchAction = null,
        Action<ApiResp<IDictionary<string, StockQuote>>>? postBatchFetchAction = null,
        Action<Exception>? onBatchFetchException = null,
        CancellationToken cancellationToken = default)
    {
        var clonedTickers = tickers.Select(YFUtils.BuildYFTicker).ToList();
        var quotesMap = new Dictionary<string, StockQuote>();
        while (clonedTickers.Count > 0)
        {
            var currentChunk = clonedTickers.Take(5).ToList();
            clonedTickers = [.. clonedTickers.Skip(5)];
            preBatchFetchAction?.Invoke(currentChunk);

            var tickersAsCommaSeparatedList = string.Join(",", currentChunk);
            try
            {
                var finhubQuotesResult = await finHubClient.GetStockQuotesAsync(tickersAsCommaSeparatedList, cancellationToken: cancellationToken);
                postBatchFetchAction?.Invoke(finhubQuotesResult);
                foreach (var quote in finhubQuotesResult.Data ?? EmptyDict)
                {
                    quotesMap[quote.Key] = quote.Value;
                }
            }
            catch (Exception ex)
            {
                onBatchFetchException?.Invoke(ex);
            }
        }
        return quotesMap;
    }

    /// <summary>
    /// Fetches the closing prices of the given tickers one day before their dividend/distribution events.
    /// </summary>
    /// <param name="events"></param>
    /// <param name="finHubClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<IDictionary<string, decimal>> FetchPreExDivPricesAsync(
        IEnumerable<MarketEventEntity> events,
        IFinHubClient finHubClient,
        CancellationToken cancellationToken)
    {
        var preExDivPrices = new Dictionary<string, decimal>();
        var now = DateTimeOffset.UtcNow;
        var eventsToCheck = events.Where(e => e.EventTime < now)
            .Where(e => e.EventType.Equals(MarketEventEntity.EVENT_DIVIDEND, StringComparison.OrdinalIgnoreCase)
            || e.EventType.Equals(MarketEventEntity.EVENT_DISTRIBUTION, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var e in eventsToCheck)
        {
            var tz = MarketEventUtils.MarketToDefaultTimeZoneId(e.MarketId);
            var dateAt = (e.EventTime.ToTimeZoneSilently(tz) ?? e.EventTime).AddDays(-1).Date;
            var quoteAtResp = await finHubClient.GetStockQuoteAtDateAsync(
                YFUtils.BuildYFTicker(e.ItemCode),
                dateAt.ToString("yyyy-MM-dd"),
                cancellationToken: cancellationToken);
            if (quoteAtResp.Status == 200 && quoteAtResp.Data != null)
            {
                preExDivPrices[e.ItemCode] = quoteAtResp.Data.Close;
            }
        }
        return preExDivPrices;
    }
}
