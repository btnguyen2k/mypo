using System.Text.Json.Serialization;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class PortfolioEntity : Entity<string>
{
    /// <inheritdoc />
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Id of the parent portfolio, if any.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Portfolio's friendly name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Portfolio's description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base currency for the portfolio.
    /// </summary>
    public string Currency { get; set; } = default!;

    /// <summary>
    /// User Id of the portfolio owner.
    /// </summary>
    public string OwnerUserId { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public PortfolioMetadata? Metadata { get; set; }

    public override string ToString() => Name ?? string.Empty;
}

public sealed class PortfolioMetadata
{
    [JsonPropertyName("viewers"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ISet<string>? Viewers { get; set; }

    [JsonPropertyName("default_market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultMarketId { get; set; }

    [JsonPropertyName("refresh_timestamp")]
    public long MetadataRefreshTimestamp { get; set; }

    [JsonIgnore]
    public DateTime MetadataRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(MetadataRefreshTimestamp).UtcDateTime;

    [JsonPropertyName("total_costs")]
    public decimal TotalCosts { get; set; } = 0;

    [JsonPropertyName("total_market_value")]
    public decimal TotalMarketValue { get; set; } = 0;

    [JsonIgnore]
    public decimal TotalPnl => TotalCosts > 0 && TotalMarketValue > 0 ? TotalMarketValue - TotalCosts : 0;

    [JsonIgnore]
    public decimal TotalPnlPct => TotalCosts > 0 ? TotalPnl / TotalCosts : 0;

    /// <summary>
    /// When <c>true</c>, this portfolio is not used for tracking stocks directly; it acts as a
    /// parent/container portfolio only.
    /// </summary>
    [JsonPropertyName("is_container")]
    public bool IsContainer { get; set; } = false;

    /// <summary>
    /// The day considered the first day of the week (used for weekly reporting). Defaults to Monday.
    /// </summary>
    [JsonPropertyName("first_day_of_week")]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// The month considered the first month of the fiscal year (1 = January .. 12 = December, used for
    /// quarterly/yearly fiscal reporting). Defaults to January.
    /// </summary>
    [JsonPropertyName("fiscal_year_start_month")]
    public int? FiscalYearStartMonth { get; set; }

    /// <summary>Unix timestamp (seconds) of when the weekly report was last run.</summary>
    [JsonPropertyName("trun_weekly_report")]
    public long LastWeeklyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTime LastWeeklyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastWeeklyReportTimestamp).UtcDateTime;

    /// <summary>Unix timestamp (seconds) marking the first date of the last reported week.</summary>
    [JsonPropertyName("tmark_weekly_report")]
    public long WeeklyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTime WeeklyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(WeeklyReportPeriodStart).UtcDateTime;

    /// <summary>Unix timestamp (seconds) of when the monthly report was last run.</summary>
    [JsonPropertyName("trun_monthly_report")]
    public long LastMonthlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTime LastMonthlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastMonthlyReportTimestamp).UtcDateTime;

    /// <summary>Unix timestamp (seconds) marking the first date of the last reported month.</summary>
    [JsonPropertyName("tmark_monthly_report")]
    public long MonthlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTime MonthlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(MonthlyReportPeriodStart).UtcDateTime;

    /// <summary>Unix timestamp (seconds) of when the quarterly report was last run.</summary>
    [JsonPropertyName("trun_quarterly_report")]
    public long LastQuarterlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTime LastQuarterlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastQuarterlyReportTimestamp).UtcDateTime;

    /// <summary>Unix timestamp (seconds) marking the first date of the last reported quarter.</summary>
    [JsonPropertyName("tmark_quarterly_report")]
    public long QuarterlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTime QuarterlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(QuarterlyReportPeriodStart).UtcDateTime;

    /// <summary>Unix timestamp (seconds) of when the yearly (fiscal) report was last run.</summary>
    [JsonPropertyName("trun_yearly_report")]
    public long LastYearlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTime LastYearlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastYearlyReportTimestamp).UtcDateTime;

    /// <summary>Unix timestamp (seconds) marking the first date of the last reported year (fiscal).</summary>
    [JsonPropertyName("tmark_yearly_report")]
    public long YearlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTime YearlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(YearlyReportPeriodStart).UtcDateTime;
}
