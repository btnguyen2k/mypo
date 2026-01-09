using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.EF;

public sealed class PortfolioDbContextRepository : DbContext, IPortfolioRepository
{
	private readonly ICacheFacade<IPortfolioRepository>? cache;

	public PortfolioDbContextRepository(DbContextOptions<PortfolioDbContextRepository> options, ICacheFacade<IPortfolioRepository>? cache = default)
		: base(options)
	{
		this.cache = cache;
	}

	//protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	//{
	//	base.OnConfiguring(optionsBuilder);
	//}

	private void ChangeTracker_DetectedAllChanges(object? sender, DetectedChangesEventArgs e) => throw new NotImplementedException();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		new PortfolioRecEntityTypeConfiguration().Configure(modelBuilder.Entity<PortfolioRec>());
	}

	private static T PrepareForUpdate<T>(T t) where T : Entity<string>
	{
		t.UpdatedAt = DateTime.UtcNow;
		t.ConcurrencyStamp = Guid.NewGuid().ToString();
		return t;
	}

	/*----------------------------------------------------------------------*/

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
	public async ValueTask<PortfolioRec> CreatePortfolioAsync(PortfolioRec portfolioRec, CancellationToken cancellationToken = default)
	{
		var entry = await PortfolioRecStore.AddAsync(portfolioRec, cancellationToken);
		await SaveChangesAsync(cancellationToken);
		return entry.Entity;
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

	/*----------------------------------------------------------------------*/

	private DbSet<TransactionRec> TxRecStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<IEnumerable<TransactionRec>> GetTransactionsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await TxRecStore.AsNoTracking()
			.Where(tr => tr.PortfolioId == portfolioId)
			.OrderByDescending(tr => tr.Time)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<TransactionRec> CreateTxAsync(TransactionRec txRec, CancellationToken cancellationToken = default)
	{
		var entry = await TxRecStore.AddAsync(txRec, cancellationToken);
		await SaveChangesAsync(cancellationToken);
		return entry.Entity;
	}
}
