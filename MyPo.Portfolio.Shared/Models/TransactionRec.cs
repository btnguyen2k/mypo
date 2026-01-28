using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class TransactionRec : Entity<string>
{
	public const string TXTYPE_BUY = "BUY";
	public const string TXTYPE_SELL = "SELL";
	public static readonly List<string> TxTypes = [ TXTYPE_BUY, TXTYPE_SELL ];

	public const string ITEM_TYPE_STOCK = "STOCK";

	public static readonly List<string> ItemTypes = [ ITEM_TYPE_STOCK ];

	//// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();
	public string PortfolioId { get; set; } = default!;
	public string Type { get; set; } = default!;
	public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;
	public decimal Quantity { get; set; } = 0.0m;
	public decimal Price { get; set; } = 0.0m;
	public decimal FeeTx { get; set; } = 0.0m;
	public decimal FeeTax { get; set; } = 0.0m;
	public decimal FeeOther { get; set; } = 0.0m;
	public string ItemType { get; set; } = default!;
	public string ItemCode { get; set; } = default!;
	public string? MarketId { get; set; }
	public string? Notes { get; set; }
	public bool IsSettled { get; set; } = false;
	public decimal TotalFee => FeeTx + FeeTax + FeeOther;
}
