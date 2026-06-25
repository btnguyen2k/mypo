using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public enum ReportType
{
    WEEKLY,
    MONTHLY,
    QUARTERLY,
    YEARLY
}

public sealed class ReportEntity : Entity<string>
{
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
    /// ISO 8601 format: 2024-W01 for weekly, 2024-01 for monthly, 2024-Q1 for quarterly, 2024 for yearly
    /// </summary>
    public string Period { get; set; } = default!;

    public string PortfolioId { get; set; } = default!;

    /// <summary>
    /// Item code in the format EXCHANGE:SYMBOL, e.g. NASDAQ:AAPL
    /// </summary>
    public string ItemCode { get; set; } = default!;

    public decimal Quantity { get; set; }

    public decimal Cost { get; set; }

    public decimal OpenValue { get; set; }

    public decimal CloseValue { get; set; }

    public bool IsFinal { get; set; } = false;
}
