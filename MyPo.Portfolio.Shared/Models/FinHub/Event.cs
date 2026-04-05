using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class EventBase
{
	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("exchange")]
	public string Exchange { get; set; } = string.Empty;

	[JsonPropertyName("company_name")]
	public string CompanyName { get; set; } = string.Empty;

	[JsonPropertyName("timestamp")]
	public int Timestamp { get; set; }

	[JsonPropertyName("date")]
	public string TimestampStr { get; set; } = string.Empty;

	[JsonIgnore]
	public DateTimeOffset Date => !string.IsNullOrEmpty(TimestampStr)
		? DateTimeOffset.TryParse(TimestampStr, out var dt) ? dt.ToUniversalTime() : DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToUniversalTime()
		: DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToUniversalTime();

	[JsonPropertyName("event_category")]
	public string EventCategory { get; set; } = string.Empty;

	[JsonPropertyName("source_name")]
	public string SourceName { get; set; } = string.Empty;

	[JsonPropertyName("link")]
	public string Link { get; set; } = string.Empty;
}

public sealed class UpcomingDividendEvent : EventBase
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("amount")]
	public decimal Amount { get; set; }

	[JsonPropertyName("dividend_yield")]
	public decimal DividendYield { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("payment_date")]
	public string PaymentDate { get; set; } = string.Empty;

	[JsonPropertyName("analysis"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DividendEventAnalysis? Analysis { get; set; }
}

public sealed class UpcomingEarningsEvent : EventBase
{
	[JsonPropertyName("report_period")]
	public string ReportPeriod { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;
}

public sealed class ListingEvent : EventBase
{
	[JsonPropertyName("sector")]
	public string Sector { get; set; } = string.Empty;

	[JsonPropertyName("industry")]
	public string Industry { get; set; } = string.Empty;

	[JsonPropertyName("principal_activities")]
	public string PrincipalActivities { get; set; } = string.Empty;

	[JsonPropertyName("price")]
	public decimal Price { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("capital")]
	public long Capital { get; set; }
}
