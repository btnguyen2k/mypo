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
    public async ValueTask<IEnumerable<ReportEntity>> GetSnapshotReportAsync(PortfolioEntity portfolio, ReportType reportType, string reportPeriod, string symbol, CancellationToken cancellationToken = default)
    {
        symbol = string.IsNullOrEmpty(symbol) ? "*" : symbol.ToUpper();
        return await ReportStore.AsNoTracking()
            .Where(r =>
                r.PortfolioId == portfolio.Id &&
                r.Type == reportType &&
                r.PeriodStart == reportPeriod)
            .Where(r =>
                symbol == "*" || r.ItemCode == symbol
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
                    existingPortfolio.Metadata.LastWeeklyReportTimestamp = 0;
                    existingPortfolio.Metadata.LastMonthlyReportTimestamp = 0;
                    existingPortfolio.Metadata.LastQuarterlyReportTimestamp = 0;
                    existingPortfolio.Metadata.LastYearlyReportTimestamp = 0;
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
