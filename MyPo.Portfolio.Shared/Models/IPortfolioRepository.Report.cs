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
    /// <param name="portfolio">The portfolio to query snapshot report entries.</param>
    /// <param name="reportType">The report type to query.</param>
    /// <param name="reportStartDate">Query entries which have the specific report start date (in "yyyy-MM-dd" format)</param>
    /// <param name="symbol">Query entries that match the specific symbol (should be in format "EXCHANGE:SYMBOL" or <see cref="ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO"/>)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<ReportEntity>> GetSnapshotReportAsync(PortfolioEntity portfolio, ReportType reportType, string reportStartDate, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the immediate previous report record (if any) for a given report entity.
    /// </summary>
    /// <param name="reportEntity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>The previous report record will match (portfolio, report-type, item-code, tx-type) with the supplied one.</remarks>
    public ValueTask<ReportEntity?> GetPrevReportAsync(ReportEntity reportEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the most recent <see cref="ReportEntity.TX_TYPE_POSITION"> report entry per symbol (excluding the portfolio
    /// aggregate <see cref="ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO" />) that is still open (non-zero accumulated quantity)
    /// as of just before <paramref name="beforePeriodStart"/>.
    /// </summary>
    /// <param name="portfolio">The portfolio to query report entries.</param>
    /// <param name="reportType">The report type to query.</param>
    /// <param name="beforePeriodStart">Query only entries BEFORE this date (in "yyyy-MM-dd" format).</param>
    /// <remarks>
    /// Used to carry held-but-not-traded positions forward so every period marks them to market.
    /// <paramref name="beforePeriodStart"/> is the current period's PeriodStart ("yyyy-MM-dd"); entries with
    /// PeriodStart strictly less than it are considered.
    /// </remarks>
    public ValueTask<IEnumerable<ReportEntity>> GetOpenPositionsAsOfAsync(PortfolioEntity portfolio, ReportType reportType, string beforePeriodStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets (deletes) all report records for a given portfolio.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ResetReports(string portfolioId, CancellationToken cancellationToken = default);
}
