using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyPo.Portfolio.Shared.EF;
sealed class PortfolioRecEntityTypeConfiguration : GenericEntityTypeConfiguration<PortfolioRec, string>
{
	public override void Configure(EntityTypeBuilder<PortfolioRec> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}portfolio"); // change table name if needed
		builder.Property(t => t.Id).HasColumnName("portfolio_id").HasMaxLength(48);
		builder.Property(builder => builder.Name).HasColumnName("portfolio_name").HasMaxLength(64);
		builder.Property(builder => builder.Description).HasColumnName("portfolio_desc").HasMaxLength(256);
		builder.Property(builder => builder.Currency).HasColumnName("portfolio_currency").HasMaxLength(8);
		builder.Property(builder => builder.CreatedAt).HasColumnName("created_at");
		builder.Property(builder => builder.UpdatedAt).HasColumnName("updated_at");
		builder.Property(builder => builder.OwnerUserId).HasColumnName("owner_id").HasMaxLength(48);
		builder.Property(builder => builder.IsActive).HasColumnName("is_active");
	}
}
