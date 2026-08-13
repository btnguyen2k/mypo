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

    /// <summary>
    /// Unix timestamp (seconds) of when the metadata was last refreshed - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("refresh_timestamp")]
    public long MetadataRefreshTimestamp { get; set; }

    [JsonIgnore]
    public DateTimeOffset MetadataRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(MetadataRefreshTimestamp);

    [JsonPropertyName("cost_basic")]
    public decimal CostBasic { get; set; } = 0;

    [JsonPropertyName("market_value")]
    public decimal MarketValue { get; set; } = 0;

    [JsonIgnore]
    public decimal UnrealizedPnl => CostBasic > 0 && MarketValue > 0 ? MarketValue - CostBasic : 0;

    [JsonIgnore]
    public decimal UnrealizedPnlPct => CostBasic > 0 ? UnrealizedPnl / CostBasic : 0;

    [JsonPropertyName("total_buys")]
    public decimal TotalBuys { get; set; } = 0;

    [JsonPropertyName("total_sells")]
    public decimal TotalSells { get; set; } = 0;

    [JsonPropertyName("total_fees")]
    public decimal TotalFees { get; set; } = 0;

    [JsonPropertyName("total_tax")]
    public decimal TotalTax { get; set; } = 0;

    [JsonPropertyName("total_interest")]
    public decimal TotalInterest { get; set; } = 0;

    [JsonPropertyName("total_income")]
    public decimal TotalIncome { get; set; } = 0;

    [JsonIgnore]
    public decimal TotalInvestment => TotalBuys + TotalFees + TotalTax;

    [JsonIgnore]
    public decimal TotalReturn => TotalSells + TotalInterest + TotalIncome + MarketValue;

    [JsonIgnore]
    public decimal TotalPnl => TotalReturn - TotalInvestment;

    [JsonIgnore]
    public decimal TotalPnlPct => TotalInvestment > 0 ? TotalPnl / TotalInvestment : 0;

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

    /// <summary>
    /// Unix timestamp (seconds) of when the weekly report was last run - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("trun_weekly_report")]
    public long LastWeeklyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTimeOffset LastWeeklyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastWeeklyReportTimestamp);

    /// <summary>
    /// Unix timestamp (seconds) marking the first date of the last reported week - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("tmark_weekly_report")]
    public long WeeklyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTimeOffset WeeklyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(WeeklyReportPeriodStart);

    /// <summary>
    /// Unix timestamp (seconds) of when the monthly report was last run - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("trun_monthly_report")]
    public long LastMonthlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTimeOffset LastMonthlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastMonthlyReportTimestamp);

    /// <summary>
    /// Unix timestamp (seconds) marking the first date of the last reported month - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("tmark_monthly_report")]
    public long MonthlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTimeOffset MonthlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(MonthlyReportPeriodStart);

    /// <summary>
    /// Unix timestamp (seconds) of when the quarterly report was last run - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("trun_quarterly_report")]
    public long LastQuarterlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTimeOffset LastQuarterlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastQuarterlyReportTimestamp);

    /// <summary>
    /// Unix timestamp (seconds) marking the first date of the last reported quarter - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("tmark_quarterly_report")]
    public long QuarterlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTimeOffset QuarterlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(QuarterlyReportPeriodStart);

    /// <summary>
    /// Unix timestamp (seconds) of when the yearly (fiscal) report was last run - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("trun_yearly_report")]
    public long LastYearlyReportTimestamp { get; set; }

    [JsonIgnore]
    public DateTimeOffset LastYearlyReportUTC => DateTimeOffset.FromUnixTimeSeconds(LastYearlyReportTimestamp);

    /// <summary>
    /// Unix timestamp (seconds) marking the first date of the last reported year (fiscal) - stored as UTC timestamp.
    /// </summary>
    [JsonPropertyName("tmark_yearly_report")]
    public long YearlyReportPeriodStart { get; set; }

    [JsonIgnore]
    public DateTimeOffset YearlyReportPeriodStartUTC => DateTimeOffset.FromUnixTimeSeconds(YearlyReportPeriodStart);
}
