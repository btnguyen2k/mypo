using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Api.Services;

public sealed class HoldingTickerReq
{
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = "";

    [JsonPropertyName("num_shares")]
    public decimal NumShares { get; set; }

    [JsonPropertyName("avg_price")]
    public decimal AvgPrice { get; set; }

    [JsonPropertyName("market_price")]
    public decimal MarketPrice { get; set; }

    [JsonPropertyName("target_allocation")]
    public decimal TargetAllocation { get; set; }

    [JsonPropertyName("tags"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tags { get; set; }
}

public class BasePortfolioPlanReq
{
    [JsonPropertyName("country")]
    public string Country { get; set; } = "";

    [JsonPropertyName("investor_theme")]
    public string? InvestorTheme { get; set; }

    [JsonPropertyName("current_allocation")]
    public List<HoldingTickerReq> CurrentAllocation { get; set; } = [];
}

public sealed class SpotLightPortfolioReq : BasePortfolioPlanReq { }

public sealed class AnalyzePortfolioReq : BasePortfolioPlanReq
{
    [JsonPropertyName("rebalance_plan")]
    public bool BuildRebalancePlan { get; set; } = false;
}

public sealed class BuildPortfolioReq : BasePortfolioPlanReq { }

public sealed class AnalyzeTickerReq
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("intent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Intent { get; set; }
}
