using FinHub.Client.Models.Portfolios;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Utils;

public static class FinHubHelper
{
    /// <summary>
    /// Builds the FinHub request allocation list from a plan's current holdings.
    /// </summary>
    public static IReadOnlyList<PortfolioHolding> BuildAllocationReqs(PortfolioPlanEntity plan)
        => [.. (plan.Metadata?.HoldingTickers ?? []).Select(ht => new PortfolioHolding
        {
            Ticker = ht.Ticker,
            TargetAllocation = ht.TargetAllocation,
            NumShares = ht.Shares,
            AvgPrice = ht.AveragePrice,
            MarketPrice = ht.MarketPrice,
            Tags = ht.Tags,
        })];
}
