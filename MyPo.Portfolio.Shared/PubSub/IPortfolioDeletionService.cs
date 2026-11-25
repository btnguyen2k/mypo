using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.PubSub;

/// <summary>
/// Deletes a portfolio and publishes the corresponding application event.
/// </summary>
public interface IPortfolioDeletionService
{
    ValueTask<bool> DeleteAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default);
}
