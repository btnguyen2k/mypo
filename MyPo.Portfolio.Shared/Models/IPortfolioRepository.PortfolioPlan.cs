using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Gets <see cref="PortfolioPlanEntity" /> records owned by a user.
	/// </summary>
	public ValueTask<IEnumerable<PortfolioPlanEntity>> GetPortfolioPlansByOwnerUserIdAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets <see cref="PortfolioPlanEntity" /> records accessible by a user (as owner or viewer).
	/// </summary>
	public ValueTask<IEnumerable<PortfolioPlanEntity>> GetPortfolioPlansAccessibleByUserAsync(MyPoUser user, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new <see cref="PortfolioPlanEntity" /> record.
	/// </summary>
	public ValueTask<PortfolioPlanEntity?> CreatePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a <see cref="PortfolioPlanEntity" /> record by its ID.
	/// </summary>
	public ValueTask<PortfolioPlanEntity?> GetPortfolioPlanByIdAsync(string planId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing <see cref="PortfolioPlanEntity" /> record.
	/// </summary>
	public ValueTask<PortfolioPlanEntity?> UpdatePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing <see cref="PortfolioPlanEntity" /> record.
	/// </summary>
	public ValueTask<bool> DeletePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default);
}
