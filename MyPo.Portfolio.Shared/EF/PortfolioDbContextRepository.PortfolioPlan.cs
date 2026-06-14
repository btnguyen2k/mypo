using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<PortfolioPlanEntity> PortfolioPlanStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PortfolioPlanEntity>> GetPortfolioPlansByOwnerUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await PortfolioPlanStore.AsNoTracking()
            .Where(pr => pr.OwnerUserId == userId)
            .OrderBy(pr => pr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PortfolioPlanEntity>> GetPortfolioPlansAccessibleByUserAsync(MyPoUser user, CancellationToken cancellationToken = default)
    {
        var ownedPlans = await PortfolioPlanStore.AsNoTracking()
            .Where(plan => plan.OwnerUserId == user.Id)
            .ToListAsync(cancellationToken);
        var viewerPlans = await PortfolioPlanStore.AsNoTracking()
            .Join(PortfolioStore, plan => plan.PortfolioId, portfolio => portfolio.Id, (plan, portfolio) => new { Plan = plan, Portfolio = portfolio })
            .Where(rec => Microsoft.EntityFrameworkCore.EF.Functions.JsonContains(rec.Portfolio.Metadata!.Viewers!, $"\"{user.Email}\""))
            .Select(rec => rec.Plan)
            .ToListAsync(cancellationToken);
        var accessiblePlans = ownedPlans.Concat(viewerPlans).OrderBy(plan => plan.Name).DistinctBy(plan => plan.Id);
        return accessiblePlans;
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioPlanEntity?> CreatePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default)
    {
        var entry = await PortfolioPlanStore.AddAsync(plan, cancellationToken);
        return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioPlanEntity?> GetPortfolioPlanByIdAsync(string planId, CancellationToken cancellationToken = default)
    {
        return await PortfolioPlanStore.AsNoTracking().FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<PortfolioPlanEntity?> UpdatePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default)
    {
        var existingEntry = await PortfolioPlanStore.FindAsync([plan.Id], cancellationToken);
        if (existingEntry == null)
        {
            return null;
        }
        Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(plan));
        return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeletePortfolioPlanAsync(PortfolioPlanEntity plan, CancellationToken cancellationToken = default)
    {
        PortfolioPlanStore.Remove(plan);
        return await SaveChangesAsync(cancellationToken) > 0;
    }
}
