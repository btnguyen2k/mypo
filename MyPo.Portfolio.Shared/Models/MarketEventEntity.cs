using System.Text.Json.Serialization;
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

	[JsonPropertyName("exchange"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Exchange { get; set; }

	[JsonPropertyName("company_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CompanyName { get; set; }

	[JsonPropertyName("industry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Industry { get; set; }

	[JsonPropertyName("source_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? SourceName { get; set; }

	[JsonPropertyName("link"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Link { get; set; }

	[JsonPropertyName("status"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Status { get; set; }

	[JsonPropertyName("report_period"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ReportPeriod { get; set; }

	[JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public decimal? Amount { get; set; }

	[JsonPropertyName("dividend_yield"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public decimal? DividendYield { get; set; }

	[JsonPropertyName("currency"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Currency { get; set; }

	[JsonPropertyName("payment_date"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PaymentDate { get; set; }

	[JsonPropertyName("price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public decimal? Price { get; set; }

	[JsonPropertyName("capital"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Capital { get; set; }

	[JsonIgnore]
	public string CurrencySymbol => Currency?.ToUpper() switch
	{
		"DOLLAR" or "USD" or "AUD" => "$",
		"CENT" => "¢",
		"DONG" or "VND" => "₫",
		_ => string.Empty
	};
}
