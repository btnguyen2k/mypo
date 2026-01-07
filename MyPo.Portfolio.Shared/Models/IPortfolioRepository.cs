namespace MyPo.Portfolio.Shared.Models;

public interface IPortfolioRepository
{
	/// <summary>
	/// Gets portfolio records owned by a user.
	/// </summary>
	public ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserIdAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new portfolio record.
	/// </summary>
	public ValueTask<PortfolioRec> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default);
}
