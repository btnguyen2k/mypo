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
        var roiSummary = PnlSummary.New(portfolioId);
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
            ApplyTxTypeValue(roiSummary, row.TxType, row.TotalValue);
        }

        return roiSummary;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<PnlSummary>> GetPnlSummaryForPortfolioForPeriodAsync(string portfolioId, DateTimeOffset startPeriodIncl, DateTimeOffset endPeriodExcl, CancellationToken cancellationToken = default)
    {
        var summaries = new List<PnlSummary>();
        summaries.AddRange(await GetPnlSummaryForPortfolioForPeriodPerSymbolAsync(portfolioId, startPeriodIncl, endPeriodExcl, cancellationToken));
        var wholePortfolioSummary = await GetPnlSummaryForPortfolioForPeriodAllTxTypesAsync(portfolioId, startPeriodIncl, endPeriodExcl, cancellationToken);
        if (wholePortfolioSummary is not null)
        {
            summaries.Add(wholePortfolioSummary);
        }
        return summaries;
    }

    private async ValueTask<PnlSummary?> GetPnlSummaryForPortfolioForPeriodAllTxTypesAsync(string portfolioId, DateTimeOffset startPeriodIncl, DateTimeOffset endPeriodExcl, CancellationToken cancellationToken = default)
    {
        var rows = await TxSettlementStore.AsNoTracking()
            .Where(rr => rr.PortfolioId == portfolioId)
            .Where(rr => rr.Status != TxSettlementEntity.STATUS_ARCHIVED)
            .Where(rr => rr.TxTime >= startPeriodIncl.ToUniversalTime() && rr.TxTime < endPeriodExcl.ToUniversalTime())
            .GroupBy(rr => rr.TxType)
            .Select(g => new
            {
                TxType = g.Key,
                TotalValue = g.Sum(rr => rr.TxValue)
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        var summary = PnlSummary.New(portfolioId);
        foreach (var row in rows)
        {
            ApplyTxTypeValue(summary, row.TxType, row.TotalValue);
        }
        return summary;
    }

    private async ValueTask<IEnumerable<PnlSummary>> GetPnlSummaryForPortfolioForPeriodPerSymbolAsync(string portfolioId, DateTimeOffset startPeriodIncl, DateTimeOffset endPeriodExcl, CancellationToken cancellationToken = default)
    {
        // Aggregate settlement values and originating buy/sell quantities in a single grouped query.
        // Buy/sell quantities come from the originating transactions, obtained by left joining
        // TxSettlementStore (S) with TxBuySellStore (BS) on S.RefTxId = BS.Id. The join is on BS's
        // primary key, so it never multiplies settlement rows and the S.TxValue sums stay correct.
        var rows = await (
            from s in TxSettlementStore.AsNoTracking()
            where s.PortfolioId == portfolioId
                  && s.Status != TxSettlementEntity.STATUS_ARCHIVED
                  && s.TxTime >= startPeriodIncl.ToUniversalTime() && s.TxTime < endPeriodExcl.ToUniversalTime()
            join bs in TxBuySellStore.AsNoTracking() on s.RefTxId equals bs.Id into bsj
            from bs in bsj.DefaultIfEmpty()
            group new { s, bs } by new { s.TxType, s.RefMarketId, s.RefItemCode } into g
            select new
            {
                g.Key.TxType,
                g.Key.RefMarketId,
                g.Key.RefItemCode,
                TotalQuantity = g.Sum(x => x.bs != null ? x.bs.Quantity : 0.0m),
                TotalValue = g.Sum(x => x.s.TxValue)
            })
            .ToListAsync(cancellationToken);

        // One PnlSummary per (market, item) grouping; transaction types are folded into each summary.
        var summaries = new Dictionary<(string?, string?), PnlSummary>();
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.RefItemCode))
            {
                // skip aggregate summaries for non-specific items
                continue;
            }

            var key = (row.RefMarketId, row.RefItemCode);
            if (!summaries.TryGetValue(key, out var roiSummary))
            {
                roiSummary = PnlSummary.New(portfolioId);
                roiSummary.RefMarketId = row.RefMarketId;
                roiSummary.RefItemCode = row.RefItemCode;
                summaries[key] = roiSummary;
            }
            ApplyTxTypeValue(roiSummary, row.TxType, row.TotalValue, row.TotalQuantity);
        }

        return summaries.Values;
    }

    /// <summary>
    /// Folds a settlement <paramref name="value"/> (and optional buy/sell <paramref name="quantity"/>) into the
    /// matching <see cref="PnlSummary"/> field, based on the settlement transaction type. Values are accumulated
    /// (<c>+=</c>); the quantity is only relevant to BUY/SELL types.
    /// </summary>
    private static void ApplyTxTypeValue(PnlSummary summary, string txType, decimal value, decimal quantity = 0m)
    {
        switch (txType)
        {
            case TxSettlementEntity.TX_TYPE_BUY:
                summary.TotalBuyValue += value;
                summary.TotalBuyQuantity += quantity;
                break;
            case TxSettlementEntity.TX_TYPE_SELL:
                summary.TotalSellValue += value;
                summary.TotalSellQuantity += quantity;
                break;
            case TxSettlementEntity.TX_TYPE_DIVIDEND:
                summary.TotalDividends += value;
                break;
            case TxSettlementEntity.TX_TYPE_DISTRIBUTION:
                summary.TotalDistributions += value;
                break;
            case TxSettlementEntity.TX_TYPE_TAX:
                summary.TotalTax += value;
                break;
            case TxSettlementEntity.TX_TYPE_INTEREST:
                summary.TotalInterest += value;
                break;
            case TxSettlementEntity.TX_TYPE_FEE:
                summary.TotalFees += value;
                break;
            case TxSettlementEntity.TX_TYPE_CASHIN:
                summary.TotalCashIn += value;
                break;
            case TxSettlementEntity.TX_TYPE_CASHOUT:
                summary.TotalCashOut += value;
                break;
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<TxSettlementEntity>> GetTxSettlementsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DEBUG] GetTxSettlementsByPortfolioIdAsync({portfolioId})");
        return await TxSettlementStore.AsNoTracking()
            .Where(rr => rr.PortfolioId == portfolioId).Where(rr => rr.Status != TxSettlementEntity.STATUS_ARCHIVED)
            .OrderByDescending(rr => rr.TxTime).ThenBy(rr => rr.RefItemCode).ThenBy(rr => rr.TxType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<TxSettlementEntity?> GetFirstTxSettlementByPortfolioId(string portfolioId, CancellationToken cancellationToken = default)
    {
        return await TxSettlementStore.AsNoTracking()
            .Where(rr => rr.PortfolioId == portfolioId)
            .Where(rr => rr.Status != TxSettlementEntity.STATUS_ARCHIVED)
            .OrderBy(rr => rr.TxTime)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
