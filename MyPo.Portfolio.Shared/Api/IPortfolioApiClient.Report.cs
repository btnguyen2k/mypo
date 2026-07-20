using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
    public const string API_REPORT_RESET = "/api/report_reset/{portfolioId}";

    /// <summary>
    /// Calls the API <see cref="API_REPORT_RESET"/> to reset (delete) all report records for a given portfolio.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp> ResetReportsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_REPORT_PERIODS = "/api/report_periods/{type}";

    /// <summary>
    /// Calls the API <see cref="API_REPORT_PERIODS"/> to get report periods for a given portfolio of a given type.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="type">WEEKLY, MONTHLY, QUARTERLY or YEARLY</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<ReportPeriod>>> GetReportPeriodsAsync(string portfolioId, ReportType type, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_REPORT_SNAPSHOT = "/api/report_snapshot/{type}";

    /// <summary>
    /// Calls the API <see cref="API_REPORT_SNAPSHOT"/> to get report snapshot for a given symbol from a portfolio of a given type and period start date.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="type">WEEKLY, MONTHLY, QUARTERLY or YEARLY</param>
    /// <param name="periodStart">yyyy-MM-dd</param>
    /// <param name="symbol">The stock symbol to get report snapshot, or empty (or <see cref="ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO"/>) for entire portfolio</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A single snapshot report entry if <paramref name="symbol"/> specifies a single symbol; or all snapshot report entries for the period if <paramref name="symbol"/> is <see cref="ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO"/></returns>
    public Task<ApiResp<IEnumerable<ReportResp>>> GetReportSnapshotAsync(string portfolioId, ReportType type, string periodStart, string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

    public const string API_REPORT_TREND = "/api/report_trend/{type}";

    /// <summary>
    /// Calls the API <see cref="API_REPORT_TREND"/> to get the value/P&amp;L trend (a time series of the
    /// last <paramref name="count"/> aggregate report entries) for a given portfolio, report type and symbol;
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="type">WEEKLY, MONTHLY, QUARTERLY or YEARLY</param>
    /// <param name="symbol">Query entries that match the specific symbol (should be in format "EXCHANGE:SYMBOL" or <see cref="ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO"/>)</param>
    /// <param name="count">The maximum number of most-recent periods to return.</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The aggregate entries ordered by period start ascending (oldest first).</returns>
    public Task<ApiResp<IEnumerable<ReportResp>>> GetReportTrendAsync(string portfolioId, ReportType type, string symbol, int count, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
