namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
    /// <summary>
    /// Save (upsert) a report record.
    /// </summary>
    /// <param name="report"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<bool> SaveReportAsync(ReportEntity report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save (upsert) multiple report records in a batch.
    /// </summary>
    /// <param name="reports"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<bool> SaveReportsAsync(IEnumerable<ReportEntity> reports, CancellationToken cancellationToken = default);

    // public ValueTask<ReportEntity?> GetSnapshotReportAsync(PortfolioEntity portfolio, ReportType reportType, string reportPeriod, CancellationToken cancellationToken = default);
}
