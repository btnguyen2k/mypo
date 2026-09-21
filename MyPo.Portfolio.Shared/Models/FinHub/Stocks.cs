using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public sealed class StockQuote
{
    [JsonPropertyName("currency"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Currency { get; set; }

    [JsonPropertyName("market_price")]
    public decimal MarketPrice { get; set; }

    [JsonPropertyName("market_price_change"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MarketPriceChange { get; set; }

    [JsonPropertyName("market_price_change_percent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MarketPriceChangePercent { get; set; }

    [JsonPropertyName("market_open"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MarketOpen { get; set; }

    [JsonPropertyName("market_day_high"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MarketDayHigh { get; set; }

    [JsonPropertyName("market_day_low"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MarketDayLow { get; set; }

    [JsonPropertyName("fifty_two_week_high"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? FiftyTwoWeekHigh { get; set; }

    [JsonPropertyName("fifty_two_week_low")]
    public decimal? FiftyTwoWeekLow { get; set; }

    [JsonPropertyName("market_volume"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MarketVolume { get; set; }

    [JsonPropertyName("bid"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Bid { get; set; }

    [JsonPropertyName("bid_size"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BidSize { get; set; }

    [JsonPropertyName("ask")]
    public decimal? Ask { get; set; }

    [JsonPropertyName("ask_size"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AskSize { get; set; }

    [JsonPropertyName("market_cap"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MarketCap { get; set; }

    [JsonPropertyName("trailing_eps"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TrailingEps { get; set; }

    [JsonPropertyName("forward_eps"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ForwardEps { get; set; }

    [JsonPropertyName("trailing_p_e"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TrailingPE { get; set; }

    [JsonPropertyName("forward_p_e"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ForwardPE { get; set; }

    [JsonPropertyName("beta"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Beta { get; set; }

    [JsonPropertyName("recommendation_key"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RecommendationKey { get; set; }

    [JsonPropertyName("target_high_price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TargetHighPrice { get; set; }

    [JsonPropertyName("target_low_price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TargetLowPrice { get; set; }

    [JsonPropertyName("target_mean_price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TargetMeanPrice { get; set; }

    [JsonPropertyName("target_median_price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TargetMedianPrice { get; set; }

    /// <summary>
    /// 0: No change
    /// -1: Down
    /// 1: Up
    /// </summary>
    [JsonIgnore]
    public int MarketPriceStatus => MarketPriceChange == 0 ? 0 : (MarketPriceChange < 0 ? -1 : 1);

    /// <summary>
    /// 0: No change
    /// -1: Down
    /// 1: Up
    /// </summary>
    [JsonIgnore]
    public int EpsStatus => TrailingEps == ForwardEps ? 0 : (TrailingEps > ForwardEps ? -1 : 1);
}
