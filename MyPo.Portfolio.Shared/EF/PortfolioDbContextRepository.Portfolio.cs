using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<PortfolioEntity> PortfolioStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PortfolioEntity>> GetPortfoliosByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await PortfolioStore.AsNoTracking()
            .Where(pr => pr.OwnerUserId == userId)
            .OrderBy(pr => pr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PortfolioEntity>> GetPortfoliosByUserAsync(MyPoUser user, CancellationToken cancellationToken = default)
    {
        var ownedPortfolios = await PortfolioStore.AsNoTracking()
            .Where(pr => pr.OwnerUserId == user.Id)
            .OrderBy(pr => pr.Name)
            .ToListAsync(cancellationToken);
        var viewerPortfolios = await PortfolioStore.AsNoTracking()
            .Where(pr => Microsoft.EntityFrameworkCore.EF.Functions.JsonContains(pr.Metadata!.Viewers!, $"\"{user.Email}\""))
            .OrderBy(pr => pr.Name)
            .ToListAsync(cancellationToken);
        return ownedPortfolios.Concat(viewerPortfolios).DistinctBy(pr => pr.Id);
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioEntity?> CreatePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default)
    {
        var entry = await PortfolioStore.AddAsync(portfolio, cancellationToken);
        return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioEntity?> GetPortfolioByIdAsync(string portfolioId, CancellationToken cancellationToken = default)
    {
        return await PortfolioStore.AsNoTracking().FirstOrDefaultAsync(pr => pr.Id == portfolioId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioEntity?> UpdatePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default)
    {
        var existingEntry = await PortfolioStore.FindAsync([portfolio.Id], cancellationToken);
        if (existingEntry == null)
        {
            return null;
        }
        Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(portfolio));
        return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeletePortfolioAsync(PortfolioEntity portfolio, CancellationToken cancellationToken = default)
    {
        PortfolioStore.Remove(portfolio);
        return await SaveChangesAsync(cancellationToken) > 0;
    }
}
