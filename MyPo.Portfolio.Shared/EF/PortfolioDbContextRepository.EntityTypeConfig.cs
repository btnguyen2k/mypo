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
		builder.Property(e => e.Id).HasColumnName("portfolio_id").HasMaxLength(48);
		builder.Property(p => p.ParentId).HasColumnName("parent_id").HasMaxLength(48);
		builder.Property(p => p.Name).HasColumnName("portfolio_name").HasMaxLength(64);
		builder.Property(p => p.Description).HasColumnName("portfolio_desc").HasMaxLength(256);
		builder.Property(p => p.Currency).HasColumnName("portfolio_currency").HasMaxLength(8);
		builder.Property(p => p.OwnerUserId).HasColumnName("owner_id").HasMaxLength(48);
		builder.Property(p => p.IsActive).HasColumnName("is_active");
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}

sealed class TransactionRecEntityTypeConfiguration : GenericEntityTypeConfiguration<TransactionRec, string>
{
	public override void Configure(EntityTypeBuilder<TransactionRec> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}transactions"); // change table name if needed
		builder.Property(p => p.Id).HasColumnName("tx_id").HasMaxLength(48);
		builder.Property(p => p.PortfolioId).HasColumnName("portfolio_id").HasMaxLength(48);
		builder.Property(p => p.Type).HasColumnName("tx_type").HasMaxLength(4);
		builder.Property(p => p.Time).HasColumnName("tx_time");
		builder.Property(p => p.Quantity).HasColumnName("tx_quantity");
		builder.Property(p => p.Price).HasColumnName("tx_price");
		builder.Property(p => p.Notes).HasColumnName("tx_notes").HasMaxLength(256);
		builder.Property(p => p.FeeTx).HasColumnName("fee_tx");
		builder.Property(p => p.FeeTax).HasColumnName("fee_tax");
		builder.Property(p => p.FeeOther).HasColumnName("fee_other");
		builder.Property(p => p.ItemType).HasColumnName("item_type").HasMaxLength(16);
		builder.Property(p => p.ItemCode).HasColumnName("item_code").HasMaxLength(16);
		builder.Property(p => p.MarketId).HasColumnName("market_id").HasMaxLength(16);
		builder.Property(p => p.IsSettled).HasColumnName("is_settled");
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}
