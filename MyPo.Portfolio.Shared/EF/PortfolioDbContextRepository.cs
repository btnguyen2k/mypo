using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

	/*----------------------------------------------------------------------*/
}
