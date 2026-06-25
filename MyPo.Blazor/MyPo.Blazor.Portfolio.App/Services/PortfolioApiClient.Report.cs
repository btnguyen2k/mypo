using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<string>>> GetReportPeriodsAsync(string portfolioId, ReportType type, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "pid", portfolioId },
        };
        var endpoint = IPortfolioApiClient.API_REPORT_PERIODS.Replace("{type}", type.ToString(), StringComparison.OrdinalIgnoreCase);
        endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<string>>(httpResult, cancellationToken);
    }
}
