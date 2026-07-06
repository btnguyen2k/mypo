using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Buffers.Binary;
using System.Text.Json;
using MyPo.Shared.Helpers;
using Ddth.Signum;

namespace MyPo.Portfolio.Shared.EF;

sealed class PortfolioEntityTypeConfiguration : GenericEntityTypeConfiguration<PortfolioEntity, string>
{
    public override void Configure(EntityTypeBuilder<PortfolioEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}portfolio"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("portfolio_id");
        builder.Property(e => e.ParentId).HasColumnName("parent_id");
        builder.Property(e => e.Name).HasColumnName("portfolio_name");
        builder.Property(e => e.Description).HasColumnName("portfolio_desc");
        builder.Property(e => e.Currency).HasColumnName("portfolio_currency");
        builder.Property(e => e.OwnerUserId).HasColumnName("owner_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.Metadata).HasColumnName("portfolio_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<PortfolioMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<PortfolioMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(PortfolioMetadata? a, PortfolioMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(PortfolioMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static PortfolioMetadata? MetadataSnapshot(PortfolioMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<PortfolioMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class PortfolioPlanEntityTypeConfiguration : GenericEntityTypeConfiguration<PortfolioPlanEntity, string>
{
    public override void Configure(EntityTypeBuilder<PortfolioPlanEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}portfolio_plans"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("plan_id");
        builder.Property(e => e.Type).HasColumnName("plan_type");
        builder.Property(e => e.OwnerUserId).HasColumnName("owner_id");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.Name).HasColumnName("plan_name");
        builder.Property(e => e.Metadata).HasColumnName("plan_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<PortfolioPlanMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<PortfolioPlanMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(PortfolioPlanMetadata? a, PortfolioPlanMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(PortfolioPlanMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static PortfolioPlanMetadata? MetadataSnapshot(PortfolioPlanMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<PortfolioPlanMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class TxBuySellEntityTypeConfiguration : GenericEntityTypeConfiguration<TxBuySellEntity, string>
{
    public override void Configure(EntityTypeBuilder<TxBuySellEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}buys_sells"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("tx_id");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.Type).HasColumnName("tx_type");
        builder.Property(e => e.Time).HasColumnName("tx_time");
        builder.Property(e => e.Quantity).HasColumnName("tx_quantity");
        builder.Property(e => e.Price).HasColumnName("tx_price");
        builder.Property(e => e.Notes).HasColumnName("tx_notes");
        builder.Property(e => e.FeeTx).HasColumnName("fee_tx");
        builder.Property(e => e.FeeTax).HasColumnName("fee_tax");
        builder.Property(e => e.FeeOther).HasColumnName("fee_other");
        builder.Property(e => e.ItemType).HasColumnName("item_type");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.MarketId).HasColumnName("market_id");
        builder.Property(e => e.IsSettled).HasColumnName("is_settled");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }
}

sealed class AssetEntityTypeConfiguration : GenericEntityTypeConfiguration<AssetEntity, string>
{
    public override void Configure(EntityTypeBuilder<AssetEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}ownings"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("owning_id");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.ItemType).HasColumnName("item_type");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.MarketId).HasColumnName("market_id");
        builder.Property(e => e.Quantity).HasColumnName("item_quantity");
        builder.Property(e => e.AveragePrice).HasColumnName("average_price");
        builder.Property(e => e.Metadata).HasColumnName("item_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<AssetMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<AssetMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(AssetMetadata? a, AssetMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(AssetMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static AssetMetadata? MetadataSnapshot(AssetMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<AssetMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class TxSettlementEntityTypeConfiguration : GenericEntityTypeConfiguration<TxSettlementEntity, string>
{
    public override void Configure(EntityTypeBuilder<TxSettlementEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}settlements"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("tx_id");
        builder.Property(e => e.Status).HasColumnName("tx_status");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.TxType).HasColumnName("tx_type");
        builder.Property(e => e.TxTime).HasColumnName("tx_time");
        builder.Property(e => e.TxValue).HasColumnName("tx_value");
        builder.Property(e => e.RefTxId).HasColumnName("ref_tx_id");
        builder.Property(e => e.RefItemType).HasColumnName("ref_item_type");
        builder.Property(e => e.RefItemCode).HasColumnName("ref_item_code");
        builder.Property(e => e.RefMarketId).HasColumnName("ref_market_id");
        builder.Property(e => e.TxDesc).HasColumnName("tx_desc");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }
}

sealed class SymbolAnalysisEntityTypeConfiguration : GenericEntityTypeConfiguration<SymbolAnalysisEntity, string>
{
    public override void Configure(EntityTypeBuilder<SymbolAnalysisEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}symbol_analysis"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("analysis_id");
        builder.Property(e => e.OwnerId).HasColumnName("owner_id");
        builder.Property(e => e.MarketId).HasColumnName("market_id");
        builder.Property(e => e.ItemType).HasColumnName("item_type");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.AnalysisType).HasColumnName("analysis_type");
        builder.Property(e => e.AnalysisTime).HasColumnName("analysis_time");
        builder.Property(e => e.AnalysisPrompt).HasColumnName("analysis_prompt");
        builder.Property(e => e.AnalysisResult).HasColumnName("analysis_result");
        builder.Property(e => e.Metadata).HasColumnName("analysis_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<SymbolAnalysisMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<SymbolAnalysisMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(SymbolAnalysisMetadata? a, SymbolAnalysisMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(SymbolAnalysisMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static SymbolAnalysisMetadata? MetadataSnapshot(SymbolAnalysisMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<SymbolAnalysisMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class CheckpointEntityTypeConfiguration : GenericEntityTypeConfiguration<CheckpointEntity, string>
{
    public override void Configure(EntityTypeBuilder<CheckpointEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}checkpoints"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("checkpoint_id");
        builder.Property(e => e.OwnerId).HasColumnName("owner_id");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.MarketId).HasColumnName("market_id");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.CheckpointType).HasColumnName("checkpoint_type");
        builder.Property(e => e.CheckpointTime).HasColumnName("checkpoint_time");
        builder.Property(e => e.Metadata).HasColumnName("checkpoint_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<CheckpointMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<CheckpointMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(CheckpointMetadata? a, CheckpointMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(CheckpointMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static CheckpointMetadata? MetadataSnapshot(CheckpointMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<CheckpointMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class MarketEventEntityTypeConfiguration : GenericEntityTypeConfiguration<MarketEventEntity, string>
{
    public override void Configure(EntityTypeBuilder<MarketEventEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}market_events"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("event_id");
        builder.Property(e => e.OwnerId).HasColumnName("owner_id");
        builder.Property(e => e.MarketId).HasColumnName("market_id");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.EventType).HasColumnName("event_type");
        builder.Property(e => e.EventTime).HasColumnName("event_time");
        builder.Property(e => e.Metadata).HasColumnName("event_metadata")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonHelper.SafeDeserialize<MarketEventMetadata>(v)
            )
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<MarketEventMetadata?>(
                (a, b) => MetadataEquals(a, b),
                v => MetadataHashCode(v),
                v => MetadataSnapshot(v)
            ));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }

    private static bool MetadataEquals(MarketEventMetadata? a, MarketEventMetadata? b)
        => Signum.Checksum(a, XxHash128Hasher.Factory).AsSpan().SequenceEqual(Signum.Checksum(b, XxHash128Hasher.Factory));

    private static int MetadataHashCode(MarketEventMetadata? v)
        => v is null ? 0 : BinaryPrimitives.ReadInt32LittleEndian(Signum.Checksum(v, XxHash128Hasher.Factory));

    private static MarketEventMetadata? MetadataSnapshot(MarketEventMetadata? v)
        => v is null ? null : JsonHelper.SafeDeserialize<MarketEventMetadata>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null));
}

sealed class ReportEntityTypeConfiguration : GenericEntityTypeConfiguration<ReportEntity, string>
{
    public override void Configure(EntityTypeBuilder<ReportEntity> builder)
    {
        base.Configure(builder);
        builder.ToTable($"{Globals.TABLE_PREFIX}report"); // change table name if needed
        builder.Property(e => e.Id).HasColumnName("report_id");
        builder.Property(e => e.Type).HasConversion<string>().HasColumnName("report_type");
        builder.Property(e => e.PeriodStart).HasColumnName("report_period_start");
        builder.Property(e => e.PeriodLabel).HasColumnName("report_period");
        builder.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(e => e.ItemCode).HasColumnName("item_code");
        builder.Property(e => e.TxType).HasColumnName("tx_type");
        builder.Property(e => e.Quantity).HasColumnName("item_quantity");
        builder.Property(e => e.Cost).HasColumnName("item_cost");
        builder.Property(e => e.OpenValue).HasColumnName("open_value");
        builder.Property(e => e.CloseValue).HasColumnName("close_value");
        builder.Property(e => e.IsFinal).HasColumnName("is_final");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
    }
}
