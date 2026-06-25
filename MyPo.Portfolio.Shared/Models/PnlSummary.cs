
namespace MyPo.Portfolio.Shared.Models;

public sealed class PnlSummary
{
    public static PnlSummary New(string portfolioId) => new()
    {
        PortfolioId = portfolioId,
        TotalBuyValue = 0.0m,
        TotalBuyQuantity = 0.0m,
        TotalSellValue = 0.0m,
        TotalSellQuantity = 0.0m,
        TotalDividends = 0.0m,
        TotalDistributions = 0.0m,
        TotalTax = 0.0m,
        TotalFees = 0.0m,
        TotalCashIn = 0.0m,
        TotalCashOut = 0.0m,
        TotalInterest = 0.0m,
    };

    public string PortfolioId { get; set; } = default!;
    public string? RefMarketId { get; set; }
    public string? RefItemCode { get; set; }
    public decimal TotalBuyValue { get; set; } = 0.0m;
    public decimal TotalBuyQuantity { get; set; } = 0.0m;
    public decimal TotalSellValue { get; set; } = 0.0m;
    public decimal TotalSellQuantity { get; set; } = 0.0m;
    public decimal TotalDividends { get; set; } = 0.0m;
    public decimal TotalDistributions { get; set; } = 0.0m;
    public decimal TotalTax { get; set; } = 0.0m;
    public decimal TotalFees { get; set; } = 0.0m;
    public decimal TotalCashIn { get; set; } = 0.0m;
    public decimal TotalCashOut { get; set; } = 0.0m;
    public decimal TotalInterest { get; set; } = 0.0m;
}
