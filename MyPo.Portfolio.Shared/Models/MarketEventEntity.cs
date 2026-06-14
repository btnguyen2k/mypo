using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class MarketEventEntity : Entity<string>
{
    public const string NON_OWNER = "*";
    public const string NON_MARKET = "*";
    public const string NON_ITEM = "*";

    public const string EVENT_EARNINGS = "EARNINGS";
    public const string EVENT_DIVIDEND = "DIVIDEND";
    public const string EVENT_DISTRIBUTION = "DISTRIBUTION";
    public const string EVENT_LISTING = "LISTING";
    public static readonly IEnumerable<string> ALL_EVENTS = [EVENT_EARNINGS, EVENT_DIVIDEND, EVENT_DISTRIBUTION, EVENT_LISTING];

    /// <inheritdoc />
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    public string OwnerId { get; set; } = string.Empty;

    public string MarketId { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset EventTime { get; set; } = DateTimeOffset.UtcNow;

    public MarketEventMetadata? Metadata { get; set; }
}

public sealed class MarketEventMetadata
{
    /* Common Attrs */

    [JsonPropertyName("exchange"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Exchange { get; set; }

    [JsonPropertyName("company_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompanyName { get; set; }

    [JsonPropertyName("sector"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sector { get; set; }

    [JsonPropertyName("industry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Industry { get; set; }

    [JsonPropertyName("capital")]
    public long Capital { get; set; }

    [JsonPropertyName("source_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceName { get; set; }

    [JsonPropertyName("link"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    [JsonPropertyName("status"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonPropertyName("currency"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Currency { get; set; }

    [JsonIgnore]
    public string CurrencySymbol => Currency?.ToUpper() switch
    {
        "DOLLAR" or "USD" or "AUD" => "$",
        "CENT" => "¢",
        "DONG" or "VND" => "₫",
        _ => string.Empty
    };

    /* END Common Attrs */

    [JsonPropertyName("earnings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarketEventEarningsMetadata? Earnings { get; set; }

    [JsonPropertyName("dividend"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarketEventDividendMetadata? Dividend { get; set; }

    [JsonPropertyName("listing"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarketEventListingMetadata? Listing { get; set; }
}

public sealed class MarketEventEarningsMetadata
{
    [JsonPropertyName("report_period"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReportPeriod { get; set; }
}

public sealed class MarketEventDividendMetadata
{
    [JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Amount { get; set; }

    [JsonPropertyName("dividend_yield"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DividendYield { get; set; }

    [JsonPropertyName("payment_date"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("analysis"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DividendEventAnalysis? Analysis { get; set; }
}

public sealed class MarketEventListingMetadata
{
    [JsonPropertyName("price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Price { get; set; }

    [JsonPropertyName("analysis"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ListingEventAnalysis? Analysis { get; set; }
}
