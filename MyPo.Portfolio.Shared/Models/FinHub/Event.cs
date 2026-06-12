using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Models.FinHub;

public class EventBase
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("exchange"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Exchange { get; set; }

    [JsonPropertyName("company_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompanyName { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }

    [JsonPropertyName("date"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TimestampStr { get; set; }

    [JsonIgnore]
    public DateTimeOffset Date => !string.IsNullOrEmpty(TimestampStr)
        ? DateTimeOffset.TryParse(TimestampStr, out var dt) ? dt.ToUniversalTime() : DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToUniversalTime()
        : DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToUniversalTime();

    [JsonPropertyName("event_category"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EventCategory { get; set; }

    [JsonPropertyName("source_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceName { get; set; }

    [JsonPropertyName("link"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
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

    [JsonPropertyName("payment_date"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("analysis"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DividendEventAnalysis? Analysis { get; set; }
}

public sealed class UpcomingEarningsEvent : EventBase
{
    [JsonPropertyName("report_period"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReportPeriod { get; set; }

    [JsonPropertyName("status"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
}

public sealed class ListingEvent : EventBase
{
    [JsonPropertyName("sector"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sector { get; set; }

    [JsonPropertyName("industry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Industry { get; set; }

    [JsonPropertyName("principal_activities"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrincipalActivities { get; set; }

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
    [JsonPropertyName("status"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonPropertyName("data_quality"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DataQuality { get; set; }

    [JsonPropertyName("search_findings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SearchFindings { get; set; }

    [JsonPropertyName("stance"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Stance { get; set; }

    [JsonPropertyName("catalyst"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Catalyst { get; set; }

    [JsonPropertyName("risks"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? Risks { get; set; }

    [JsonPropertyName("outlook"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, ListingOutlook>? Outlook { get; set; }
}

public sealed class ListingOutlook
{
    [JsonPropertyName("direction"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; set; }

    [JsonPropertyName("reason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("confidence")]
    public int Confidence { get; set; }
}
