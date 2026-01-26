using System.Text.Json.Serialization;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class PortfolioRec : Entity<string>
{
	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Id of the parent portfolio, if any.
	/// </summary>
	public string? ParentId { get; set; }

	/// <summary>
	/// Portfolio's friendly name.
	/// </summary>
	public string Name { get; set; } = default!;

	/// <summary>
	/// Portfolio's description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Base currency for the portfolio.
	/// </summary>
	public string Currency { get; set; } = default!;

	/// <summary>
	/// User Id of the portfolio owner.
	/// </summary>
	public string OwnerUserId { get; set; } = default!;

	public bool IsActive { get; set; } = true;

	public PortfolioMetadata? Metadata { get; set; }

	public override string ToString() => Name ?? string.Empty;
}

public sealed class PortfolioMetadata
{
	[JsonPropertyName("viewers"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ISet<string>? Viewers { get; set; }
}
