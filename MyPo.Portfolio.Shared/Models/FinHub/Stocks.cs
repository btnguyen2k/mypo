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

	[JsonPropertyName("market_volume")]
	public int MarketVolume { get; set; }

	[JsonIgnore]
	public int MarketPriceStatus => MarketPriceChange == 0 ? 0 : (MarketPriceChange < 0 ? -1 : 1);
}

public sealed class SymbolInfo : SymbolBase
{
	[JsonPropertyName("overview"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SymbolOverview? Overview { get; set; }

	[JsonPropertyName("stock_quote"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StockQuote? StockQuote { get; set; }
}
