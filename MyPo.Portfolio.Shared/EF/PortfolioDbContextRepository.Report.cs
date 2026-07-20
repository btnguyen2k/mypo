using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<ReportEntity> ReportStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<bool> SaveReportAsync(ReportEntity report, CancellationToken cancellationToken = default)
    {
        var existingReport = await ReportStore
            .FirstOrDefaultAsync(r =>
                r.PortfolioId == report.PortfolioId &&
                r.Type == report.Type &&
                r.PeriodStart == report.PeriodStart &&
                r.ItemCode == report.ItemCode &&
                r.TxType == report.TxType,
                cancellationToken);

        if (existingReport != null)
        {
            // Update existing report
            existingReport = PrepareForUpdate(existingReport);
            existingReport.Metadata = report.Metadata;
            existingReport.IsFinal = report.IsFinal;
            existingReport.PeriodLabel = report.PeriodLabel; // Update the period label in case it has changed
        }
        else
        {
            // Insert new report
            await ReportStore.AddAsync(report, cancellationToken);
        }

        var changes = await SaveChangesAsync(cancellationToken);
        return changes > 0;
    }

    /// <inheritdoc />
    public async ValueTask<bool> SaveReportsAsync(IEnumerable<ReportEntity> reports, CancellationToken cancellationToken = default)
    {
        var reportList = reports.ToList();
        if (reportList.Count == 0)
        {
            return true;
        }

        using var tx = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var saved = false;
            foreach (var report in reportList)
            {
                saved |= await SaveReportAsync(report, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return saved;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ReportEntity>> GetSnapshotReportAsync(PortfolioEntity portfolio, ReportType reportType, string reportStartDate, string symbol, CancellationToken cancellationToken = default)
    {
        symbol = string.IsNullOrEmpty(symbol) ? ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO : symbol.ToUpper();
        return await ReportStore.AsNoTracking()
            .Where(r =>
                r.PortfolioId == portfolio.Id &&
                r.Type == reportType &&
                r.PeriodStart == reportStartDate)
            .Where(r =>
                symbol == ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO || r.ItemCode == symbol
            )
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ReportEntity?> GetPrevReportAsync(ReportEntity reportEntity, CancellationToken cancellationToken = default)
    {
        return await ReportStore.AsNoTracking()
            .Where(r =>
                r.PortfolioId == reportEntity.PortfolioId &&
                r.Type == reportEntity.Type &&
                r.ItemCode == reportEntity.ItemCode &&
                r.TxType == reportEntity.TxType &&
                string.Compare(r.PeriodStart, reportEntity.PeriodStart) < 0)
            .OrderByDescending(r => r.PeriodStart)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ReportEntity>> GetOpenPositionsAsOfAsync(PortfolioEntity portfolio, ReportType reportType, string beforePeriodStart, CancellationToken cancellationToken = default)
    {
        // TODO: review the implementation when data grows large

        // DB-side filter: all prior POSITION item entries (jsonb quantity is filtered in memory below).
        var candidates = await ReportStore.AsNoTracking()
            .Where(r =>
                r.PortfolioId == portfolio.Id &&
                r.Type == reportType &&
                r.TxType == ReportEntity.TX_TYPE_POSITION &&
                r.ItemCode != ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO &&
                string.Compare(r.PeriodStart, beforePeriodStart) < 0)
            .ToListAsync(cancellationToken);

        // In memory: keep only the latest entry per symbol, then drop fully-closed positions.
        return [.. candidates
            .GroupBy(r => r.ItemCode)
            .Select(g => g.OrderByDescending(x => x.PeriodStart).First())
            .Where(r => (r.Metadata?.AccumulatedQuantity ?? 0m) != 0m)];
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ReportEntity>> GetReportTrendAsync(PortfolioEntity portfolio, ReportType reportType, string symbol, int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return [];
        }

        symbol = string.IsNullOrEmpty(symbol) ? ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO : symbol.ToUpper();
        // Fetch the most-recent 'count' whole-portfolio aggregate entries (PeriodStart is "yyyy-MM-dd" so
        // string ordering is chronological), then reverse in memory to return them oldest-first for plotting.
        var latest = await ReportStore.AsNoTracking()
            .Where(r =>
                r.PortfolioId == portfolio.Id &&
                r.Type == reportType &&
                r.ItemCode == symbol)
            .OrderByDescending(r => r.PeriodStart)
            .Take(count)
            .ToListAsync(cancellationToken);
        latest.Reverse();
        return latest;
    }

    /// <inheritdoc />
    public async Task ResetReports(string portfolioId, CancellationToken cancellationToken = default)
    {
        using (var tx = await Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var existingPortfolio = await PortfolioStore.FindAsync([ portfolioId ], cancellationToken);
                if (existingPortfolio is not null)
                {
                    existingPortfolio.Metadata ??= new();
                    existingPortfolio.Metadata.LastWeeklyReportTimestamp = existingPortfolio.Metadata.WeeklyReportPeriodStart = 0;
                    existingPortfolio.Metadata.LastMonthlyReportTimestamp = existingPortfolio.Metadata.MonthlyReportPeriodStart = 0;
                    existingPortfolio.Metadata.LastQuarterlyReportTimestamp = existingPortfolio.Metadata.QuarterlyReportPeriodStart = 0;
                    existingPortfolio.Metadata.LastYearlyReportTimestamp = existingPortfolio.Metadata.YearlyReportPeriodStart = 0;
                    await SaveChangesAsync(cancellationToken);

                    await ReportStore.Where(r => r.PortfolioId == portfolioId).ExecuteDeleteAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
