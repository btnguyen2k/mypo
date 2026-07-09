namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
    /// <summary>
    /// Saves (upsert) a report record.
    /// </summary>
    /// <param name="report"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<bool> SaveReportAsync(ReportEntity report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (upsert) multiple report records in a batch.
    /// </summary>
    /// <param name="reports"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<bool> SaveReportsAsync(IEnumerable<ReportEntity> reports, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches report snapshot for a given symbol from a portfolio of a given type and period start date.
    /// </summary>
    /// <param name="portfolio"></param>
    /// <param name="reportType"></param>
    /// <param name="reportPeriod"></param>
    /// <param name="symbol"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<ReportEntity>> GetSnapshotReportAsync(PortfolioEntity portfolio, ReportType reportType, string reportPeriod, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the immediate previous report record (if any) for a given report entity.
    /// </summary>
    /// <param name="reportEntity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>The previous report record will match (portfolio, report-type, item-code, tx-type) with the supplied one.</remarks>
    public ValueTask<ReportEntity?> GetPrevReportAsync(ReportEntity reportEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets (deletes) all report records for a given portfolio.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ResetReports(string portfolioId, CancellationToken cancellationToken = default);
}
