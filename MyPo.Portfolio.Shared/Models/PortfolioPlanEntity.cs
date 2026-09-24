using System.Text.Json.Serialization;
using Ddth.Signum;
using FinHub.Client.Models.Portfolios;
using MyPo.Shared.Api;
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

public sealed class PortfolioPlanMetadata : ISignumFingerprintable
{
    /// <inheritdoc/>
    public void WriteFingerprint(IFingerprintWriter writer)
    {
        var checksumObj = new
        {
            Description,
            Holdings = HoldingTickers.ToArray(),
        };
        writer.Write(checksumObj);
    }

    /// <summary>
    /// Calculates the checksum of the current holdings data, which can be used to compare with
    /// <see cref="ChecksumLastDeepAnalysis"/> or <see cref="ChecksumLastSpotlightAnalysis"/> to determine
    /// if the holdings have changed since the last analysis/spotlight.
    /// </summary>
    /// <returns></returns>
    public string CalcChecksum()
    {
        return Signum.ChecksumHex(this, XxHash128Hasher.Factory);
    }

    [JsonPropertyName("trefresh_holdings")]
    public long HoldingsRefreshTimestamp { get; set; }

    [JsonIgnore]
    public DateTime HoldingsRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(HoldingsRefreshTimestamp).UtcDateTime;

    [JsonPropertyName("holdings")]
    public IList<HoldingTicker> HoldingTickers { get; set; } = [];

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    /*----------------------------------------------------------------------*/

    /// <summary>
    /// A checksum of the plan's holdings data, saved when the last analysis ran,
    /// used to detect changes and avoid unnecessary re-analysis when the holdings haven't changed.
    /// </summary>
    [JsonPropertyName("cksum_deep_anlys")]
    public string? ChecksumLastDeepAnalysis { get; set; }

    /// <summary>
    /// The timestamp of the analysis refresh, used to track when the last analysis was performed.
    /// </summary>
    [JsonPropertyName("t_deep_anlys")]
    public long RefreshTimestampDeepAnalysis { get; set; }

    /// <summary>
    /// The UTC datetime representation of the analysis refresh timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime RefreshUtcDeepAnalysis => DateTimeOffset.FromUnixTimeSeconds(RefreshTimestampDeepAnalysis).UtcDateTime;

    // // [JsonPropertyName("analysis")]
    // // public string Analysis { get; set; } = string.Empty;
    // [JsonPropertyName("rebalance_plan")]
    // public string RebalancePlan { get; set; } = string.Empty;

    [JsonPropertyName("deep_anlys"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IPortfolioAnalysisResult? DeepAnalysis { get; set; }

    /// <summary>
    /// The asynchronous task information for the analysis request.
    /// </summary>
    [JsonPropertyName("req_deep_anlys"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AsyncTaskInfo? RequestDeepAnalysis { get; set; }

    /// <summary>
    /// The checksum of the analysis request, used to detect changes and avoid unnecessary re-requests.
    /// </summary>
    [JsonPropertyName("cksum_req_deep_anlys")]
    public string ChecksumRequestDeepAnalysis { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the analysis request, used to track when the request was made.
    /// </summary>
    [JsonPropertyName("t_req_deep_anlys")]
    public long TimestampRequestDeepAnalysis { get; set; }

    /// <summary>
    /// The UTC datetime representation of the analysis request timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime RequestUtcDeepAnalysis => DateTimeOffset.FromUnixTimeSeconds(TimestampRequestDeepAnalysis).UtcDateTime;

    /*----------------------------------------------------------------------*/

    /// <summary>
    /// A checksum of the plan's holdings data, saved when the last spotlight analysis ran,
    /// used to detect changes and avoid unnecessary re-analysis when the holdings haven't changed.
    /// </summary>
    [JsonPropertyName("cksum_spotlight_anlys")]
    public string? ChecksumLastSpotlightAnalysis { get; set; }

    /// <summary>
    /// The timestamp of the spotlight refresh, used to track when the last spotlight analysis was performed.
    /// </summary>
    [JsonPropertyName("t_spotlight_anlys")]
    public long RefreshTimestampSpotlightAnalysis { get; set; }

    /// <summary>
    /// The UTC datetime representation of the spotlight refresh timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime RefreshUtcSpotlightAnalysis => DateTimeOffset.FromUnixTimeSeconds(RefreshTimestampSpotlightAnalysis).UtcDateTime;

    [JsonPropertyName("spotlight_anlys"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PortfolioSpotlightAnalysis? SpotlightAnalysis { get; set; }

    /// <summary>
    /// The asynchronous task information for the spotlight request.
    /// </summary>
    [JsonPropertyName("req_spotlight_anlys"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AsyncTaskInfo? RequestSpotlightAnalysis { get; set; }

    /// <summary>
    /// The checksum of the spotlight request, used to detect changes and avoid unnecessary re-requests.
    /// </summary>
    [JsonPropertyName("cksum_req_spotlight_anlys")]
    public string ChecksumRequestSpotlightAnalysis { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the spotlight request, used to track when the request was made.
    /// </summary>
    [JsonPropertyName("t_req_spotlight_anlys")]
    public long TimestampRequestSpotlightAnalysis { get; set; }

    /// <summary>
    /// The UTC datetime representation of the spotlight request timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime RequestUtcSpotlightAnalysis => DateTimeOffset.FromUnixTimeSeconds(TimestampRequestSpotlightAnalysis).UtcDateTime;
}

public sealed class HoldingTicker : ISignumFingerprintable
{
    /// <inheritdoc/>
    public void WriteFingerprint(IFingerprintWriter writer)
    {
        writer.Write(Ticker);
        writer.Write(TargetAllocation);
        writer.Write(Tags);
        writer.Write(Shares);
        writer.Write(AveragePrice);
        writer.Write(MarketPrice);
        writer.Write(DividendYield);
        writer.Write(PayoutFrequency);
    }

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
