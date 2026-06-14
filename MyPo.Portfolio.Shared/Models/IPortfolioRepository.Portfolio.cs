using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
    /// <summary>
    /// Gets <see cref="PortfolioEntity" /> records owned by a user.
    /// </summary>
    public ValueTask<IEnumerable<PortfolioEntity>> GetPortfoliosByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets <see cref="PortfolioEntity" /> records accessible by a user (as owner or viewer).
    /// </summary>
    public ValueTask<IEnumerable<PortfolioEntity>> GetPortfoliosByUserAsync(MyPoUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="PortfolioEntity" /> record.
    /// </summary>
    public ValueTask<PortfolioEntity?> CreatePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a <see cref="PortfolioEntity" /> record by its ID.
    /// </summary>
    public ValueTask<PortfolioEntity?> GetPortfolioByIdAsync(string portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing <see cref="PortfolioEntity" /> record.
    /// </summary>
    public ValueTask<PortfolioEntity?> UpdatePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing <see cref="PortfolioEntity" /> record.
    /// </summary>
    public ValueTask<bool> DeletePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default);
}
