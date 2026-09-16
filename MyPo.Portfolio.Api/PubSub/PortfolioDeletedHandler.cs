using System.Data.Common;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.PubSub;
using Wolverine.Attributes;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace MyPo.Portfolio.Api.PubSub;

/// <summary>
/// Handles the deletion of a portfolio by removing its associated checkpoints and logging the operation.
/// </summary>
/// <param name="portfolioRepository"></param>
/// <param name="logger"></param>
/// <remarks>Wolverine will automatically discover this handler in the specified assembly.</remarks>
[StickyHandler("portfolio-deleted-checkpoints")]
public sealed class PostPortfolioDeletionCheckpointsCleanupHandler(
    IPortfolioRepository portfolioRepository,
    ILogger<PostPortfolioDeletionCheckpointsCleanupHandler> logger)
{
    public static void Configure(HandlerChain chain)
    {
        chain.OnException<DbException>()
            .OrInner<DbException>()
            .Or<TimeoutException>()
            .ScheduleRetry(
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2*4),
                TimeSpan.FromSeconds(2*4*4),
                TimeSpan.FromSeconds(2*4*4*4),
                TimeSpan.FromSeconds(2*4*4*4*4))
            .Then.MoveToErrorQueue();
    }

    public async Task Handle(PortfolioDeletedEvent message, CancellationToken cancellationToken)
    {
        var deletedCount = await portfolioRepository.DeleteCheckpointsByPortfolioIdAsync(
            message.Portfolio.Id,
            cancellationToken);

        logger.LogInformation(
            "Handled portfolio deletion event {MessageId} for portfolio {PortfolioId}: removed {DeletedCount} checkpoint(s).",
            message.MessageId,
            message.Portfolio.Id,
            deletedCount);
    }
}
