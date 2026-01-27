using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Gets portfolio records owned by a user.
	/// </summary>
	public ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserIdAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets portfolio records accessible by a user (as owner or viewer).
	/// </summary>
	public ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserAsync(MyPoUser user, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new portfolio record.
	/// </summary>
	public ValueTask<PortfolioRec?> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a portfolio record by its ID.
	/// </summary>
	public ValueTask<PortfolioRec?> GetPortfolioByIdAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing portfolio record.
	/// </summary>
	public ValueTask<PortfolioRec?> UpdatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing portfolio record.
	/// </summary>
	public ValueTask<bool> DeletePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);
}
