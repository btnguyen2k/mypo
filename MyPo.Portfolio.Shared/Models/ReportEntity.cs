using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public enum ReportType
{
    UNKNOWN,
    WEEKLY,
    MONTHLY,
    QUARTERLY,
    YEARLY
}

public sealed class ReportEntity : Entity<string>
{
    // public ReportEntity New(ReportType type, DateTimeOffset periodStart, PnlSummary? summary = null)
    // {
    //     var report = new ReportEntity
    //     {
    //         Type = type,
    //         PeriodStart = periodStart.ToUniversalTime().ToString("yyyy-MM-dd"),
    //         Period = type switch
    //         {
    //             ReportType.WEEKLY => periodStart.ToUniversalTime().ToString("yyyy-\\Www"),
    //             ReportType.MONTHLY => periodStart.ToUniversalTime().ToString("yyyy-MM"),
    //             ReportType.QUARTERLY => $"Q{((periodStart.Month - 1) / 3) + 1}",
    //             ReportType.YEARLY => periodStart.ToUniversalTime().ToString("yyyy"),
    //             _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    //         },
    //         PortfolioId = summary?.PortfolioId ?? string.Empty,
    //         ItemCode = summary?.ItemCode ?? string.Empty,
    //         Quantity = summary?.Quantity ?? 0,
    //         Cost = summary?.Cost ?? 0,
    //         OpenValue = summary?.OpenValue ?? 0,
    //         CloseValue = summary?.CloseValue ?? 0,
    //         IsFinal = false
    //     };
    //     return report;
    // }

    /// <inheritdoc />
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Report type: WEEKLY, MONTHLY, QUARTERLY, YEARLY
    /// </summary>
    public ReportType Type { get; set; } = default!;

    /// <summary>
    /// YYYY-MM-DD, mark the start date of the report period
    /// </summary>
    public string PeriodStart { get; set; } = default!;

    /// <summary>
    /// Period label: FY2024-25-W01 for weekly, 2024-01 for monthly, FY2024-25-Q1 for quarterly, FY2024-25 for yearly
    /// </summary>
    public string PeriodLabel { get; set; } = default!;

    public string PortfolioId { get; set; } = default!;

    /// <summary>
    /// Item code in the format EXCHANGE:SYMBOL, e.g. NASDAQ:AAPL
    /// </summary>
    public string ItemCode { get; set; } = default!;

    /// <summary>
    /// Transaction type: BUY, SELL, DIVIDEND, etc.
    /// </summary>
    public string TxType { get; set; } = default!;

    public decimal Quantity { get; set; }

    public decimal Cost { get; set; }

    public decimal OpenValue { get; set; }

    public decimal CloseValue { get; set; }

    public bool IsFinal { get; set; } = false;
}
