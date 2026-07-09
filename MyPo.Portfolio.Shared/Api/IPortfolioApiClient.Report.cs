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
    /// <param name="symbol">The stock symbol to get report snapshot, or empty (or "*") for entire portfolio</param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<ReportEntity>>> GetReportSnapshotAsync(string portfolioId, ReportType type, string periodStart, string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
