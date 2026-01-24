using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyPo.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository : DbContext, IPortfolioRepository
{
	private readonly ICacheFacade<IPortfolioRepository>? cache;
	private ILogger<PortfolioDbContextRepository>? logger;

	public PortfolioDbContextRepository(
		DbContextOptions<PortfolioDbContextRepository> options,
		ICacheFacade<IPortfolioRepository>? cache = default,
		ILogger<PortfolioDbContextRepository>? logger = default
		)
		: base(options)
	{
		this.cache = cache;
		this.logger = logger;
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
		new TransactionRecEntityTypeConfiguration().Configure(modelBuilder.Entity<TransactionRec>());
		new AssetEntityTypeConfiguration().Configure(modelBuilder.Entity<Asset>());
		new RoiRecEntityTypeConfiguration().Configure(modelBuilder.Entity<RoiRec>());
	}

	private static T PrepareForUpdate<T>(T t) where T : Entity<string>
	{
		t.UpdatedAt = DateTime.UtcNow;
		t.ConcurrencyStamp = Guid.NewGuid().ToString();
		return t;
	}
}
