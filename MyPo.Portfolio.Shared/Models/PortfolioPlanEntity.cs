using System.Text.Json.Serialization;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;


public sealed class PortfolioPlanEntity : Entity<string>
{
    public const string PLAN_TYPE_ALLOCATION = "ALLOCATION";
    public const string PLAN_TYPE_PL = "P&L";

    public static readonly IEnumerable<string> ValidPlanTypes = [PLAN_TYPE_ALLOCATION, PLAN_TYPE_PL];

    /// <inheritdoc />
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of the plan, which can be either <see cref="PLAN_TYPE_ALLOCATION"/> or <see cref="PLAN_TYPE_PL"/>.
    /// </summary>
    public string Type { get; set; } = PLAN_TYPE_ALLOCATION;

    /// <summary>
    /// Id of the portfolio owner, which is the user id.
    /// </summary>
    public string OwnerUserId { get; set; } = default!;

    /// <summary>
    /// Id of the associated portfolio, if any.
    /// </summary>
    public string? PortfolioId { get; set; }

    /// <summary>
    /// Plan's friendly name.
    /// </summary>
    public string Name { get; set; } = default!;

    public PortfolioPlanMetadata? Metadata { get; set; }

    public override string ToString() => Name ?? string.Empty;
}

public sealed class PortfolioPlanMetadata
{
    [JsonPropertyName("trefresh_holdings")]
    public long HoldingsRefreshTimestamp { get; set; }

    [JsonIgnore]
    public DateTime HoldingsRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(HoldingsRefreshTimestamp).UtcDateTime;

    [JsonPropertyName("holdings")]
    public IList<HoldingTicker> HoldingTickers { get; set; } = [];

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("trefresh_analysis")]
    public long AnalysisRefreshTimestsmp { get; set; }

    [JsonIgnore]
    public DateTime AnalysisRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(AnalysisRefreshTimestsmp).UtcDateTime;

    [JsonPropertyName("analysis")]
    public string Analysis { get; set; } = string.Empty;
}

public sealed class HoldingTicker
{
    [JsonPropertyName("id")]
    public string Id = Guid.NewGuid().ToString();

    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonPropertyName("allocation")]
    public decimal TargetAllocation { get; set; } = 0;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("shares")]
    public decimal Shares { get; set; }

    [JsonPropertyName("avg_price")]
    public decimal AveragePrice { get; set; }

    [JsonPropertyName("market_price")]
    public decimal MarketPrice { get; set; }

    [JsonPropertyName("div_yield")]
    public decimal DividendYield { get; set; }

    [JsonPropertyName("payout_frequency")]
    public int PayoutFrequency { get; set; }

    [JsonIgnore]
    public decimal EstYearlyDividend => MarketPrice * DividendYield / 100m * Shares;
}
