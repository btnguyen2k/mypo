using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class PortfolioPlanEntity : Entity<string>
{
	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Id of the portfolio owner, which is the user id.
	/// </summary>
	public string OwnerUserId { get; set; } = default!;

	/// <summary>
	/// Id of the associated portfolio, if any.
	/// </summary>
	public string? PortfolioId { get; set; }

	/// <summary>
	/// Plan's friendly name.
	/// </summary>
	public string Name { get; set; } = default!;

	public PortfolioPlanMetadata? Metadata { get; set; }

	public override string ToString() => Name ?? string.Empty;
}

public sealed class PortfolioPlanMetadata
{
}
