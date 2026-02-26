using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class CheckpointEntity : Entity<string>
{
	public const string NON_OWNER = "*";
	public const string NON_PORTFOLIO = "*";
	public const string NON_MARKET = "*";
	public const string NON_ITEM = "*";
	public const string CHECKPOINT_INCOMING_EARNINGS = "INCOMING-EARNINGS";
	public const string CHECKPOINT_INCOMING_DIVIDEND = "INCOMING-DIVIDEND";

	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	public string OwnerId { get; set; } = string.Empty;

	public string PortfolioId { get; set; } = string.Empty;

	public string MarketId { get; set; } = string.Empty;

	public string ItemCode { get; set; } = string.Empty;

	public string CheckpointType { get; set; } = string.Empty;

	public DateTimeOffset CheckpointTime { get; set; } = DateTimeOffset.UtcNow;

	public CheckpointMetadata? Metadata { get; set; }
}

public sealed class CheckpointMetadata
{
}
