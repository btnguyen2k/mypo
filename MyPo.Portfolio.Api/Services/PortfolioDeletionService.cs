using Microsoft.EntityFrameworkCore;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.PubSub;
using Wolverine.EntityFrameworkCore;

namespace MyPo.Portfolio.Api.Services;

/// <summary>
/// Service responsible for handling the deletion of portfolios and publishing related events.
/// </summary>
/// <param name="portfolioRepository"></param>
/// <param name="publisher"></param>
public sealed class PortfolioDeletionService(
    IPortfolioRepository portfolioRepository,
    IPubSubPublisher publisher) : IPortfolioDeletionService
{
    public async ValueTask<bool> DeleteAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default)
    {
        var deleted = await portfolioRepository.DeletePortfolioAsync(portfolio, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await publisher.PublishAsync(new PortfolioDeletedEvent(portfolio), cancellationToken);
        return true;
    }
}

/// <summary>
/// Service responsible for handling the deletion of portfolios using a transactional outbox mechanism and publishing related events.
/// </summary>
/// <param name="portfolioRepository"></param>
/// <param name="outbox"></param>
public sealed class DurablePortfolioDeletionService(
    IPortfolioRepository portfolioRepository,
    IDbContextOutbox outbox) : IPortfolioDeletionService
{
    public async ValueTask<bool> DeleteAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (portfolioRepository is not DbContext dbContext)
        {
            throw new InvalidOperationException(
                $"{nameof(IPortfolioRepository)} must be backed by an EF Core DbContext to use the transactional outbox.");
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            outbox.Enroll(dbContext);
            var deleted = await portfolioRepository.DeletePortfolioAsync(portfolio, cancellationToken);
            if (!deleted)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            await outbox.PublishAsync(new PortfolioDeletedEvent(portfolio));
            await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken); // tx is committed by SaveChangesAndFlushMessagesAsync
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
