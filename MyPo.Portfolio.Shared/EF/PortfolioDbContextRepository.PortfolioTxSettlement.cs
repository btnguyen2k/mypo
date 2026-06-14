using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<TxSettlementEntity> TxSettlementStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<TxSettlementEntity?> CreateTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default)
    {
        var entry = await TxSettlementStore.AddAsync(tx, cancellationToken);
        return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
    }

    /// <inheritdoc />
    public async ValueTask<TxSettlementEntity?> GetTxSettlementByIdAsync(string txid, CancellationToken cancellationToken = default)
    {
        return await TxSettlementStore.AsNoTracking().FirstOrDefaultAsync(rr => rr.Id == txid, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<TxSettlementEntity?> UpdateTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default)
    {
        var existingEntry = await TxSettlementStore.FindAsync([tx.Id], cancellationToken);
        if (existingEntry == null)
        {
            return null;
        }
        Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(tx));
        return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteTxSettlementAsync(TxSettlementEntity tx, CancellationToken cancellationToken = default)
    {
        TxSettlementStore.Remove(tx);
        return await SaveChangesAsync(cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async ValueTask<PnlSummary> GetPnlSummaryForPortfolioAsync(string portfolioId, CancellationToken cancellationToken = default)
    {
        var roiSummary = new PnlSummary()
        {
            PortfolioId = portfolioId,
            TotalBuyValue = 0.0m,
            TotalSellValue = 0.0m,
            TotalDividends = 0.0m,
            TotalDistributions = 0.0m,
            TotalTax = 0.0m,
            TotalFees = 0.0m,
            TotalCashIn = 0.0m,
            TotalCashOut = 0.0m,
            TotalInterest = 0.0m,
        };
        var rows = await TxSettlementStore.AsNoTracking()
            .Where(rr => rr.PortfolioId == portfolioId)
            .Where(rr => rr.Status != TxSettlementEntity.STATUS_ARCHIVED)
            .GroupBy(rr => rr.TxType)
            .Select(g => new
            {
                TxType = g.Key,
                TotalValue = g.Sum(rr => rr.TxValue)
            })
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            switch (row.TxType)
            {
                case TxSettlementEntity.TX_TYPE_BUY:
                    roiSummary.TotalBuyValue = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_SELL:
                    roiSummary.TotalSellValue = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_DIVIDEND:
                    roiSummary.TotalDividends = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_DISTRIBUTION:
                    roiSummary.TotalDistributions = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_TAX:
                    roiSummary.TotalTax = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_INTEREST:
                    roiSummary.TotalInterest = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_FEE:
                    roiSummary.TotalFees = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_CASHIN:
                    roiSummary.TotalCashIn = row.TotalValue;
                    break;
                case TxSettlementEntity.TX_TYPE_CASHOUT:
                    roiSummary.TotalCashOut = row.TotalValue;
                    break;
            }
        }

        return roiSummary;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<TxSettlementEntity>> GetTxSettlementsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
    {
        return await TxSettlementStore.AsNoTracking()
            .Where(rr => rr.PortfolioId == portfolioId).Where(rr => rr.Status != TxSettlementEntity.STATUS_ARCHIVED)
            .OrderByDescending(rr => rr.TxTime)
            .ToListAsync(cancellationToken);
    }
}
