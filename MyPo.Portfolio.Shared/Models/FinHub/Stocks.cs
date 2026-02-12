using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class SymbolBase
{
	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("exchange")]
	public string Exchange { get; set; } = string.Empty;
}

public sealed class HistoryValueDaily
{
	[JsonPropertyName("timestamp")]
	public long Timestamp { get; set; }

	[JsonIgnore]
	public DateTime Date => DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;

	[JsonPropertyName("value")]
	public decimal Value { get; set; }
}

public sealed class SymbolOverview
{
	[JsonPropertyName("country"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Country { get; set; }

	[JsonPropertyName("long_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? LongName { get; set; }

	[JsonPropertyName("short_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ShortName { get; set; }

	[JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[JsonPropertyName("website"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Website { get; set; }

	[JsonPropertyName("quote_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? QuoteType { get; set; }

	[JsonPropertyName("industry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Industry { get; set; }

	[JsonPropertyName("sector"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Sector { get; set; }

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
}

public sealed class SymbolDividend
{
	[JsonPropertyName("dividend_rate")]
	public decimal DividendRate { get; set; }

	[JsonPropertyName("dividend_yield")]
	public decimal DividendYield { get; set; }

	[JsonPropertyName("ex_dividend_date")]
	public long ExDividendTimestamp { get; set; }
	[JsonIgnore]
	public DateTime ExDividendDate => DateTimeOffset.FromUnixTimeSeconds(ExDividendTimestamp).DateTime;

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
	[JsonIgnore]
	public DateTime LastDividendDate => DateTimeOffset.FromUnixTimeSeconds(LastDividendTimestamp).DateTime;
}

public sealed class StockQuote
{
	[JsonPropertyName("market_price")]
	public decimal MarketPrice { get; set; }

	[JsonPropertyName("market_price_change")]
	public decimal MarketPriceChange { get; set; }

	[JsonPropertyName("market_price_change_percent")]
	public decimal MarketPriceChangePercent { get; set; }

	[JsonPropertyName("market_open")]
	public decimal MarketOpen { get; set; }

	[JsonPropertyName("market_day_high")]
	public decimal MarketDayHigh { get; set; }

	[JsonPropertyName("market_day_low")]
	public decimal MarketDayLow { get; set; }

	[JsonPropertyName("fifty_two_week_high")]
	public decimal FiftyTwoWeekHigh { get; set; }

	[JsonPropertyName("fifty_two_week_low")]
	public decimal FiftyTwoWeekLow { get; set; }

	[JsonPropertyName("market_volume")]
	public int MarketVolume { get; set; }

	[JsonPropertyName("market_cap")]
	public long MarketCap { get; set; }

	[JsonPropertyName("bid")]
	public decimal Bid { get; set; }
	[JsonPropertyName("bid_size")]
	public int BidSize { get; set; }

	[JsonPropertyName("ask")]
	public decimal Ask { get; set; }
	[JsonPropertyName("ask_size")]
	public int AskSize { get; set; }

	[JsonPropertyName("trailing_eps")]
	public decimal TrailingEps { get; set; }

	[JsonPropertyName("forward_eps")]
	public decimal ForwardEps { get; set; }

	[JsonPropertyName("trailing_p_e")]
	public decimal TrailingPE { get; set; }

	[JsonPropertyName("forward_p_e")]
	public decimal ForwardPE { get; set; }

	[JsonPropertyName("beta")]
	public decimal Beta { get; set; }

	[JsonPropertyName("recommendation_key"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RecommendationKey { get; set; }

	[JsonPropertyName("target_high_price")]
	public decimal TargetHighPrice { get; set; }

	[JsonPropertyName("target_low_price")]
	public decimal TargetLowPrice { get; set; }

	[JsonPropertyName("target_mean_price")]
	public decimal TargetMeanPrice { get; set; }

	[JsonPropertyName("target_median_price")]
	public decimal TargetMedianPrice { get; set; }

	[JsonIgnore]
	public int MarketPriceStatus => MarketPriceChange == 0 ? 0 : (MarketPriceChange < 0 ? -1 : 1);

	[JsonIgnore]
	public int EpsStatus => TrailingEps==ForwardEps ? 0 : (TrailingEps>ForwardEps ? -1 : 1);
}

public sealed class StockHistory
{
	[JsonPropertyName("recent_high_price")]
	public decimal RecentHighPrice { get; set; }

	[JsonPropertyName("pull_pack_percent")]
	public decimal PullBackPercent { get; set; }

	[JsonPropertyName("current_volume")]
	public long CurrentVolume { get; set; }

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

	[JsonPropertyName("rsi14_history_daily"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<HistoryValueDaily>? RSI14HistoryDaily { get; set; }
}

public sealed class SymbolInfo : SymbolBase
{
	[JsonPropertyName("overview"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SymbolOverview? Overview { get; set; }

	[JsonPropertyName("stock_quote"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StockQuote? StockQuote { get; set; }

	[JsonPropertyName("dividend"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SymbolDividend? Dividend { get; set; }

	[JsonPropertyName("stock_history"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StockHistory? StockHistory { get; set; }
}
