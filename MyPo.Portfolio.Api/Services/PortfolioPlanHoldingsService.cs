using Finhub.Client;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Portfolio.Api.Services;

/// <summary>
/// Refreshes a portfolio plan's holding tickers (latest market price, dividend info, and shares/avg-price
/// pulled from a linked portfolio's assets).
/// </summary>
public interface IPortfolioPlanHoldingsService
{
    /// <summary>
    /// Rebuilds the given holding tickers with up-to-date market/dividend data and, when a
    /// <paramref name="portfolioId"/> is supplied, the current shares/average-price of the linked
    /// portfolio's matching assets.
    /// </summary>
    /// <param name="tickers">The holding tickers to refresh.</param>
    /// <param name="portfolioId">Optional id of the linked portfolio to source shares/avg-price from.</param>
    /// <param name="cancellationToken"></param>
    Task<BuildHoldingsResult> RefreshHoldingsAsync(IEnumerable<HoldingTicker> tickers, string? portfolioId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of <see cref="IPortfolioPlanHoldingsService.RefreshHoldingsAsync"/>.
/// </summary>
/// <param name="Holdings">
/// One entry per input ticker. Tickers whose info could not be fetched keep their original values and
/// are also listed in <paramref name="FailedTickers"/>, letting each caller decide whether to fail the
/// whole operation or proceed with the stale values.
/// </param>
/// <param name="FailedTickers">The tickers whose info could not be fetched.</param>
public sealed record BuildHoldingsResult(IList<HoldingTicker> Holdings, IReadOnlyList<string> FailedTickers);

/// <inheritdoc cref="IPortfolioPlanHoldingsService"/>
public sealed class PortfolioPlanHoldingsService : IPortfolioPlanHoldingsService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IFinHubClient _finHubClient;

    public PortfolioPlanHoldingsService(IPortfolioRepository portfolioRepository, IFinHubClient finHubClient)
    {
        _portfolioRepository = portfolioRepository;
        _finHubClient = finHubClient;
    }

    /// <inheritdoc />
    public async Task<BuildHoldingsResult> RefreshHoldingsAsync(IEnumerable<HoldingTicker> tickers, string? portfolioId, CancellationToken cancellationToken = default)
    {
        var assetsMap = await BuildAssetsMapAsync(portfolioId, cancellationToken);

        var holdings = new List<HoldingTicker>();
        var failedTickers = new List<string>();
        foreach (var ticker in tickers)
        {
            var tickerInfoResp = await _finHubClient.GetStockSymbolInfoAsync(ticker.Ticker, cancellationToken: cancellationToken);
            if (!tickerInfoResp.IsSuccess || tickerInfoResp.Data is null)
            {
                // keep the original holding as-is and report the failure
                holdings.Add(ticker);
                failedTickers.Add(ticker.Ticker);
                continue;
            }
            var data = tickerInfoResp.Data;
            var ht = new HoldingTicker
            {
                Id = ticker.Id,
                Ticker = data.NormalizedSymbol,
                TargetAllocation = ticker.TargetAllocation,
                Tags = ticker.Tags?.Trim() ?? string.Empty,
                Shares = assetsMap.TryGetValue(data.NormalizedSymbol, out var asset) ? asset.Quantity : 0,
                AveragePrice = assetsMap.TryGetValue(data.NormalizedSymbol, out var asset2) ? asset2.AveragePrice : 0,
                MarketPrice = data.StockQuote?.MarketPrice ?? 0,
                DividendYield = data.Dividend?.DividendYield ?? 0,
                PayoutFrequency = data.Dividend?.PayoutFrequency ?? 0,
            };
            var country = data.Country.ToUpper();
            if (country == "VN")
            {
                // special case
                ht.MarketPrice /= 1000m;
            }
            holdings.Add(ht);
        }
        return new BuildHoldingsResult(holdings, failedTickers);
    }

    /// <summary>
    /// Builds a map of (normalized symbol -&gt; asset) for the linked portfolio's holdings, used to source
    /// the current shares/average-price of each ticker. Returns an empty map when no portfolio is linked.
    /// </summary>
    private async Task<IDictionary<string, AssetEntity>> BuildAssetsMapAsync(string? portfolioId, CancellationToken cancellationToken)
    {
        var assetsMap = new Dictionary<string, AssetEntity>();
        if (string.IsNullOrWhiteSpace(portfolioId))
        {
            return assetsMap;
        }
        var assets = await _portfolioRepository.GetAssetsByPortfolioIdAsync(portfolioId, cancellationToken);
        var assetsMarket = assets.Select(a => {
            var found = Globals.MarketsMap.TryGetValue(a.MarketId?.ToUpper() ?? string.Empty, out var market);
            return new { found, a, market };
        }).Where(x => x.found).Select(x => new { asset=x.a, x.market });
        foreach (var x in assetsMarket)
        {
            assetsMap[SymbolUtils.NormalizeSymbol(x.asset.ItemCode, x.market!)] = x.asset;
        }
        return assetsMap;
    }
}
