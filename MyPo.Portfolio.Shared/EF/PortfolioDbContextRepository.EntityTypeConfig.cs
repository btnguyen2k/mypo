using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace MyPo.Portfolio.Shared.EF;
sealed class PortfolioEntityTypeConfiguration : GenericEntityTypeConfiguration<PortfolioEntity, string>
{
	public override void Configure(EntityTypeBuilder<PortfolioEntity> builder)
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
		builder.Property(p => p.Metadata).HasColumnName("portfolio_metadata")
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<PortfolioMetadata>(v, (JsonSerializerOptions?)null)
			)
			.HasColumnType("jsonb");
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}

sealed class TxBuySellEntityTypeConfiguration : GenericEntityTypeConfiguration<TxBuySellEntity, string>
{
	public override void Configure(EntityTypeBuilder<TxBuySellEntity> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}buys_sells"); // change table name if needed
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

sealed class AssetEntityTypeConfiguration : GenericEntityTypeConfiguration<AssetEntity, string>
{
	public override void Configure(EntityTypeBuilder<AssetEntity> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}ownings"); // change table name if needed
		builder.Property(p => p.Id).HasColumnName("owning_id").HasMaxLength(48);
		builder.Property(p => p.PortfolioId).HasColumnName("portfolio_id").HasMaxLength(48);
		builder.Property(p => p.ItemType).HasColumnName("item_type").HasMaxLength(16);
		builder.Property(p => p.ItemCode).HasColumnName("item_code").HasMaxLength(16);
		builder.Property(p => p.MarketId).HasColumnName("market_id").HasMaxLength(16);
		builder.Property(p => p.Quantity).HasColumnName("item_quantity");
		builder.Property(p => p.AveragePrice).HasColumnName("average_price");
		builder.Property(p => p.Metadata).HasColumnName("item_metadata")
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<AssetMetadata>(v, (JsonSerializerOptions?)null)
			)
			.HasColumnType("jsonb");
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}

sealed class TxSettlementEntityTypeConfiguration : GenericEntityTypeConfiguration<TxSettlementEntity, string>
{
	public override void Configure(EntityTypeBuilder<TxSettlementEntity> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}settlements"); // change table name if needed
		builder.Property(p => p.Id).HasColumnName("tx_id").HasMaxLength(48);
		builder.Property(p => p.Status).HasColumnName("tx_status").HasMaxLength(8);
		builder.Property(p => p.PortfolioId).HasColumnName("portfolio_id").HasMaxLength(48);
		builder.Property(p => p.TxType).HasColumnName("tx_type").HasMaxLength(8);
		builder.Property(p => p.TxTime).HasColumnName("tx_time");
		builder.Property(p => p.TxValue).HasColumnName("tx_value");
		builder.Property(p => p.RefTxId).HasColumnName("ref_tx_id").HasMaxLength(48);
		builder.Property(p => p.RefItemType).HasColumnName("ref_item_type").HasMaxLength(16);
		builder.Property(p => p.RefItemCode).HasColumnName("ref_item_code").HasMaxLength(16);
		builder.Property(p => p.RefMarketId).HasColumnName("ref_market_id").HasMaxLength(16);
		builder.Property(p => p.TxDesc).HasColumnName("tx_desc").HasMaxLength(256);
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}

sealed class SymbolAnalysisEntityTypeConfiguration : GenericEntityTypeConfiguration<SymbolAnalysisEntity, string>
{
	public override void Configure(EntityTypeBuilder<SymbolAnalysisEntity> builder)
	{
		base.Configure(builder);
		builder.ToTable($"{Globals.TABLE_PREFIX}symbol_analysis"); // change table name if needed
		builder.Property(p => p.Id).HasColumnName("analysis_id").HasMaxLength(48);
		builder.Property(p => p.OwnerId).HasColumnName("owner_id").HasMaxLength(48);
		builder.Property(p => p.MarketId).HasColumnName("market_id").HasMaxLength(16);
		builder.Property(p => p.ItemType).HasColumnName("item_type").HasMaxLength(16);
		builder.Property(p => p.ItemCode).HasColumnName("item_code").HasMaxLength(16);
		builder.Property(p => p.AnalysisType).HasColumnName("analysis_type");
		builder.Property(p => p.AnalysisTime).HasColumnName("analysis_time");
		builder.Property(p => p.AnalysisPrompt).HasColumnName("analysis_prompt");
		builder.Property(p => p.AnalysisResult).HasColumnName("analysis_result");
		builder.Property(p => p.Metadata).HasColumnName("analysis_metadata")
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<SymbolAnalysisMetadata>(v, (JsonSerializerOptions?)null)
			)
			.HasColumnType("jsonb");
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
	}
}
