using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class SymbolBase
{
	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("normalized_symbol")]
	public string NormalizedSymbol { get; set; } = string.Empty;

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("exchange")]
	public string Exchange { get; set; } = string.Empty;

	[JsonPropertyName("country")]
	public string Country { get; set; } = string.Empty;

	// [JsonIgnore]
	// private static readonly Regex YfSuffixPattern = MyRegex();

	// /// <summary>
	// /// Normalizes a YF ticker symbol to the format exchange:symbol.
	// /// YF symbols for non-US stocks have the format SYMBOL.CC where CC is the ISO 3166-1 alpha-2 country code.
	// /// This method strips the trailing country code suffix and returns exchange:symbol.
	// /// </summary>
	// public string NormalizedSymbol()
	// {
	// 	var exchange = Exchange.ToUpper();
	// 	var symbol = Symbol.ToUpper();
	// 	// YF appends a 2-letter country code (e.g. .AX, .VN, .L, .TO) for non-US stocks
	// 	var match = YfSuffixPattern.Match(symbol);
	// 	if (match.Success)
	// 	{
	// 		return $"{exchange}:{symbol[..^3]}";
	// 	}
	// 	return $"{exchange}:{symbol}";
	// }

	// [GeneratedRegex(@"\.[A-Z]{2}$", RegexOptions.Compiled)]
	// private static partial Regex MyRegex();
}

public sealed class HistoryPoint
{
	[JsonPropertyName("timestamp")]
	public long Timestamp { get; set; }

	[JsonPropertyName("timestamp_str"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TimestampStr { get; set; }

	[JsonIgnore]
	public DateTimeOffset Date => !string.IsNullOrEmpty(TimestampStr)
		? DateTimeOffset.TryParse(TimestampStr, out var dt) ? dt : DateTimeOffset.FromUnixTimeSeconds(Timestamp)
		: DateTimeOffset.FromUnixTimeSeconds(Timestamp);

	[JsonPropertyName("currency"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Currency { get; set; }

	[JsonPropertyName("open")]
	public decimal Open { get; set; }

	[JsonPropertyName("high")]
	public decimal High { get; set; }

	[JsonPropertyName("low")]
	public decimal Low { get; set; }

	[JsonPropertyName("close")]
	public decimal Close { get; set; }

	[JsonPropertyName("volume")]
	public long Volume { get; set; }

	[JsonPropertyName("dividends")]
	public decimal Dividends { get; set; }

	[JsonPropertyName("rsi14")]
	public decimal RSI14 { get; set; }

	[JsonPropertyName("dvt")]
	public decimal DVT { get; set; } // Daily Value Traded (Approximated)
}

public class SymbolOverview : SymbolBase
{
	[JsonPropertyName("short_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ShortName { get; set; }

	[JsonPropertyName("long_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? LongName { get; set; }

	[JsonPropertyName("sector"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Sector { get; set; }

	[JsonPropertyName("industry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Industry { get; set; }

	[JsonPropertyName("website"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Website { get; set; }

	[JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[JsonPropertyName("quote_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? QuoteType { get; set; }

	[JsonPropertyName("asset_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AssetType { get; set; }

	[JsonPropertyName("total_cash")]
	public long TotalCash { get; set; }

	[JsonPropertyName("total_cash_per_share")]
	public decimal TotalCashPerShare { get; set; }

	[JsonPropertyName("total_debt")]
	public long TotalDebt { get; set; }

	[JsonPropertyName("total_debt_per_share")]
	public decimal TotalDebtPerShare { get; set; }

	[JsonPropertyName("total_revenue")]
	public long TotalRevenue { get; set; }

	[JsonPropertyName("total_revenue_per_share")]
	public decimal TotalRevenuePerShare { get; set; }

	[JsonPropertyName("ebitda")]
	public long Ebitda { get; set; }

	[JsonPropertyName("ebitda_margins")]
	public decimal EbitdaMargins { get; set; }

	[JsonPropertyName("earnings_growth")]
	public decimal EarningsGrowth { get; set; }

	[JsonPropertyName("revenue_growth")]
	public decimal RevenueGrowth { get; set; }

	[JsonPropertyName("gross_margins")]
	public decimal GrossMargins { get; set; }

	[JsonPropertyName("operating_margins")]
	public decimal OperatingMargins { get; set; }

	[JsonPropertyName("profit_margins")]
	public decimal ProfitMargins { get; set; }

	[JsonPropertyName("market_cap")]
	public long MarketCap { get; set; }

	[JsonPropertyName("cap_size"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CapSize { get; set; }

	[JsonPropertyName("market_index"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MarketIndex { get; set; }
}

public sealed class SymbolDividend
{
	/// <summary>
	/// Annual Dividend amount
	/// </summary>
	[JsonPropertyName("dividend_rate")]
	public decimal DividendRate { get; set; }

	/// <summary>
	/// Annual Dividend Yield in percentage, value = 3.45 means 3.45%
	/// </summary>
	[JsonPropertyName("dividend_yield")]
	public decimal DividendYield { get; set; }

	/// <summary>
	/// Number of times the dividend is paid out per year, value = 12 means monthly
	/// </summary>
	[JsonPropertyName("payout_frequency")]
	public int PayoutFrequency { get; set; }

	[JsonPropertyName("ex_dividend_date")]
	public long ExDividendTimestamp { get; set; }

	[JsonPropertyName("ex_dividend_date_str"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExDividendTimestampStr { get; set; }

	[JsonIgnore]
	public DateTimeOffset ExDividendDate => !string.IsNullOrEmpty(ExDividendTimestampStr)
		? DateTimeOffset.TryParse(ExDividendTimestampStr, out var dt) ? dt : DateTimeOffset.FromUnixTimeSeconds(ExDividendTimestamp)
		: DateTimeOffset.FromUnixTimeSeconds(ExDividendTimestamp);

	[JsonPropertyName("five_year_avg_dividend_yield")]
	public decimal FiveYearAvgDividendYield { get; set; }

	[JsonPropertyName("trailing_annual_dividend_rate")]
    public decimal TrailingAnnualDividendRate { get; set; }

	[JsonPropertyName("trailing_annual_dividend_yield")]
    public decimal TrailingAnnualDividendYield { get; set; }

	[JsonPropertyName("last_dividend_value")]
    public decimal LastDividendValue { get; set; }

	[JsonPropertyName("last_dividend_date")]
    public long LastDividendTimestamp { get; set; }

	[JsonPropertyName("last_dividend_date_str"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? LastDividendTimestampStr { get; set; }

	[JsonIgnore]
	public DateTimeOffset LastDividendDate => !string.IsNullOrEmpty(LastDividendTimestampStr)
		? DateTimeOffset.TryParse(LastDividendTimestampStr, out var dt) ? dt : DateTimeOffset.FromUnixTimeSeconds(LastDividendTimestamp)
		: DateTimeOffset.FromUnixTimeSeconds(LastDividendTimestamp);
}

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
	public int EpsStatus => TrailingEps==ForwardEps ? 0 : (TrailingEps>ForwardEps ? -1 : 1);
}

public sealed class StockHistory
{
	[JsonPropertyName("recent_high_price")]
	public decimal RecentHighPrice { get; set; }

	/// <summary>
	/// Value = 12.34 means 12.34%
	/// </summary>
	[JsonPropertyName("pull_pack_percent")]
	public decimal PullBackPercent { get; set; }

	[JsonPropertyName("current_volume")]
	public long CurrentVolume { get; set; }

	[JsonPropertyName("yesterday_volume")]
	public long YesterdayVolume { get; set; }

	[JsonPropertyName("average_volume_30d")]
	public long AverageVolume30d { get; set; }

	[JsonPropertyName("ma10")]
	public decimal MA10 { get; set; }

	[JsonPropertyName("ma20")]
	public decimal MA20 { get; set; }

	[JsonPropertyName("ma50")]
	public decimal MA50 { get; set; }

	[JsonPropertyName("ma100")]
	public decimal MA100 { get; set; }

	[JsonPropertyName("ma200")]
	public decimal MA200 { get; set; }

	[JsonPropertyName("rsi14")]
	public decimal RSI14 { get; set; }

	[JsonPropertyName("history_90d"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<HistoryPoint>? History90d { get; set; }
}

public sealed class SymbolInfo : SymbolOverview
{
	[JsonPropertyName("stock_quote"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StockQuote? StockQuote { get; set; }

	[JsonPropertyName("dividend"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SymbolDividend? Dividend { get; set; }

	[JsonPropertyName("stock_history"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StockHistory? StockHistory { get; set; }
}
