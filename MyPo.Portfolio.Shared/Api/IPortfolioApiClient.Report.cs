using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
    public const string API_REPORT_PERIODS = "/api/report_periods/{type}";

    /// <summary>
    /// Calls the API <see cref="API_REPORT_PERIODS"/> to get report periods for a given portfolio of a given type.
    /// </summary>
    /// <param name="portfolioId"></param>
    /// <param name="type"></param>
    /// <param name="authToken"></param>
    /// <param name="baseUrl"></param>
    /// <param name="requestHttpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResp<IEnumerable<string>>> GetReportPeriodsAsync(string portfolioId, ReportType type, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
