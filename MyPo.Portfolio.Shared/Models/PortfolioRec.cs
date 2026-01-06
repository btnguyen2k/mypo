using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class PortfolioRec : Entity<string>
{
	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Portfolio's friendly name.
	/// </summary>
	public string Name { get; set; } = default!;

	/// <summary>
	/// Portfolio's description.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Base currency for the portfolio.
	/// </summary>
	public string Currency { get; set; } = default!;

	/// <summary>
	/// User Id of the portfolio owner.
	/// </summary>
	public string OwnerUserId { get; set; } = default!;

	public bool IsActive { get; set; } = true;

	public override string ToString() => Name ?? string.Empty;
}
