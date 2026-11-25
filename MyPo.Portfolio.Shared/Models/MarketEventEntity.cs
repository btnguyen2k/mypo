using System.Text.Json.Serialization;
using FinHub.Client.Models.Earnings;
using FinHub.Client.Models.Listings;
using FinHub.Client.Models.Dividends;
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
    [JsonPropertyName("listing"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ListingEvent? Listing { get; set; }

    [JsonPropertyName("earnings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UpcomingEarningsEvent? Earnings { get; set; }

    [JsonPropertyName("dividend"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UpcomingDividendEvent? Dividend { get; set; }
}
