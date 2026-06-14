using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<SymbolAnalysisEntity> SymbolAnalysisStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<SymbolAnalysisEntity?> CreateSymbolAnalysisAsync(SymbolAnalysisEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await SymbolAnalysisStore.AddAsync(entity, cancellationToken);
        return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
    }

    /// <inheritdoc />
    public async ValueTask<SymbolAnalysisEntity?> GetSymbolAnalysisAsync(string ownerId, string marketId, string itemType, string itemCode, string analysisType, CancellationToken cancellationToken = default)
    {
        return await SymbolAnalysisStore.AsNoTracking().FirstOrDefaultAsync(e => e.OwnerId == ownerId
            && e.MarketId == marketId.ToUpper()
            && e.ItemType == itemType.ToUpper()
            && e.ItemCode == itemCode.ToUpper()
            && e.AnalysisType == analysisType.ToUpper(),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SymbolAnalysisEntity?> UpdateSymbolAnalysisAsync(SymbolAnalysisEntity entity, CancellationToken cancellationToken = default)
    {
        var existingEntry = await SymbolAnalysisStore.FindAsync([entity.Id], cancellationToken);
        if (existingEntry == null)
        {
            return null;
        }
        Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(entity));
        return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
    }
}
