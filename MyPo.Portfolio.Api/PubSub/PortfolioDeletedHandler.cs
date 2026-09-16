using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.PubSub;

namespace MyPo.Portfolio.Api.PubSub;

/// <summary>
/// Handles the deletion of a portfolio by removing its associated checkpoints and logging the operation.
/// </summary>
/// <param name="portfolioRepository"></param>
/// <param name="logger"></param>
public sealed class PortfolioDeletedHandler(
    IPortfolioRepository portfolioRepository,
    ILogger<PortfolioDeletedHandler> logger)
{
    public async Task Handle(PortfolioDeletedEvent message, CancellationToken cancellationToken)
    {
        var deletedCount = await portfolioRepository.DeleteCheckpointsByPortfolioIdAsync(
            message.PortfolioId,
            cancellationToken);

        logger.LogInformation(
            "Handled portfolio deletion event {MessageId} for portfolio {PortfolioId}: removed {DeletedCount} checkpoint(s).",
            message.MessageId,
            message.PortfolioId,
            deletedCount);
    }
}
