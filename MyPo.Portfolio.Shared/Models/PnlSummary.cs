
namespace MyPo.Portfolio.Shared.Models;

public sealed class PnlSummary
{
	public string PortfolioId { get; set; } = default!;
	public decimal TotalBuyValue { get; set; } = 0.0m;
	public decimal TotalSellValue { get; set; } = 0.0m;
	public decimal TotalDividends { get; set; } = 0.0m;
	public decimal TotalTax { get; set; } = 0.0m;
	public decimal TotalFees { get; set; } = 0.0m;
	public decimal TotalCashIn { get; set; } = 0.0m;
	public decimal TotalCashOut { get; set; } = 0.0m;
	public decimal TotalInterest { get; set; } = 0.0m;
}
