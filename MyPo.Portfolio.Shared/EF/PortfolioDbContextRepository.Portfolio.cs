using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
	private DbSet<PortfolioRec> PortfolioRecStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserIdAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await PortfolioRecStore.AsNoTracking()
			.Where(pr => pr.OwnerUserId == userId)
			.OrderBy(pr => pr.Name)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<IEnumerable<PortfolioRec>> GetPortfolioByUserAsync(MyPoUser user, CancellationToken cancellationToken = default)
	{
		var ownedPortfolios = await PortfolioRecStore.AsNoTracking()
			.Where(pr => pr.OwnerUserId == user.Id)
			.OrderBy(pr => pr.Name)
			.ToListAsync(cancellationToken);
		var viewerPortfolios = await PortfolioRecStore.AsNoTracking()
			.Where(pr => Microsoft.EntityFrameworkCore.EF.Functions.JsonContains(pr.Metadata!.Viewers!, $"\"{user.Email}\""))
			.OrderBy(pr => pr.Name)
			.ToListAsync(cancellationToken);
		return ownedPortfolios.Concat(viewerPortfolios).DistinctBy(pr => pr.Id);
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		var entry = await PortfolioRecStore.AddAsync(portfolioRec, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> GetPortfolioByIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await PortfolioRecStore.AsNoTracking().FirstOrDefaultAsync(pr => pr.Id == portfolioId, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<PortfolioRec?> UpdatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		var existingEntry = await PortfolioRecStore.FindAsync([portfolioRec.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(portfolioRec));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/// <inheritdoc />
	public async ValueTask<bool> DeletePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		PortfolioRecStore.Remove(portfolioRec);
		return await SaveChangesAsync(cancellationToken) > 0;
	}
}
