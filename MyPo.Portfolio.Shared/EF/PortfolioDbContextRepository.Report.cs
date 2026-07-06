using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
    private DbSet<ReportEntity> ReportStore { get; set; }

    /// <inheritdoc />
    public async ValueTask<bool> SaveReportAsync(ReportEntity report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report, nameof(report));

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
            existingReport.Quantity = report.Quantity;
            existingReport.Cost = report.Cost;
            existingReport.OpenValue = report.OpenValue;
            existingReport.CloseValue = report.CloseValue;
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

    public async ValueTask<bool> SaveReportsAsync(IEnumerable<ReportEntity> reports, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports, nameof(reports));

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
}
