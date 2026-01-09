using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class OwningItem : Entity<string>
{
	public string PortfolioId { get; set; } = default!;
	public string ItemType { get; set; } = default!;
	public string ItemCode { get; set; } = default!;
	public string? MarketId { get; set; }
	public decimal Quantity { get; set; } = 0.0m;
	public decimal AveragePrice { get; set; } = 0.0m;
}
