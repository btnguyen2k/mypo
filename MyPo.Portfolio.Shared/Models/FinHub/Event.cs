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

	[JsonPropertyName("analysis"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ListingEventAnalysis? Analysis { get; set; }
}

public sealed class ListingEventAnalysis
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("data_quality")]
	public string DataQuality { get; set; } = string.Empty;

	[JsonPropertyName("search_findings")]
	public string SearchFindings { get; set; } = string.Empty;

	[JsonPropertyName("stance")]
	public string Stance { get; set; } = string.Empty;

	[JsonPropertyName("catalyst")]
	public string Catalyst { get; set; } = string.Empty;

	[JsonPropertyName("risks"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IList<string>? Risks { get; set; }

	[JsonPropertyName("outlook"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IDictionary<string, ListingOutlook>? Outlook { get; set; }
}

public sealed class ListingOutlook
{
	[JsonPropertyName("direction")]
	public string Direction { get; set; } = string.Empty;

	[JsonPropertyName("reason")]
	public string Reason { get; set; } = string.Empty;

	[JsonPropertyName("confidence")]
	public int Confidence { get; set; }
}
