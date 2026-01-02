using Microsoft.AspNetCore.Identity;

namespace MyPo.Shared.Identity;

public sealed class MyPoRole : IdentityRole
{
	public IEnumerable<IdentityRoleClaim<string>>? Claims { get; set; } = default!;

	public string? Description { get; set; }

	/// <summary>
	/// Touches the entity, updating the <see cref="ConcurrencyStamp"/> property.
	/// </summary>
	public void Touch() => ConcurrencyStamp = Guid.NewGuid().ToString();

	public override bool Equals(object? obj) => obj is MyPoRole other
		&& (ReferenceEquals(this, other) || Id.Equals(other.Id, Globals.StringComparison));

	public override int GetHashCode() => Id.GetHashCode(Globals.StringComparison);
}
