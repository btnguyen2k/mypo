using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class TxSettlementEntity : Entity<string>
{
	public const string TX_TYPE_CASHIN = "CASHIN";
	public const string TX_TYPE_CASHOUT = "CASHOUT";
	public const string TX_TYPE_DIVIDEND = "DIVIDEND";
	public const string TX_TYPE_DISTRIBUTION = "DISTRIBUTION";
	public const string TX_TYPE_INTEREST = "INTEREST";
	public const string TX_TYPE_FEE = "FEE";
	public const string TX_TYPE_TAX = "TAX";
	public const string TX_TYPE_BUY = "BUY";
	public const string TX_TYPE_SELL = "SELL";

	public static readonly ISet<string> TxTypes = new HashSet<string>
	{
		TX_TYPE_CASHIN,
		TX_TYPE_CASHOUT,
		TX_TYPE_DIVIDEND,
		TX_TYPE_DISTRIBUTION,
		TX_TYPE_INTEREST,
		TX_TYPE_FEE,
		TX_TYPE_TAX,
		TX_TYPE_BUY,
		TX_TYPE_SELL,
	};

	public const string STATUS_NEW = "NEW";
	public const string STATUS_FINAL = "FINAL";
	public const string STATUS_ARCHIVED = "ARCHIVED";

	public static readonly ISet<string> ImmutableStatuses = new HashSet<string>
	{
		STATUS_FINAL,
		STATUS_ARCHIVED,
	};

	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();
	public string Status { get; set; } = STATUS_NEW;
	public string PortfolioId { get; set; } = default!;
	public string TxType { get; set; } = default!;
	public DateTimeOffset TxTime { get; set; } = DateTimeOffset.UtcNow;
	public decimal TxValue { get; set; } = 0.0m;
	public string? RefTxId { get; set; }
	public string? RefItemType { get; set; }
	public string? RefItemCode { get; set; }
	public string? RefMarketId { get; set; }
	public string? TxDesc { get; set; }
}
