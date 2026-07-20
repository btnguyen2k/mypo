using Microsoft.AspNetCore.WebUtilities;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient
{
    /// <inheritdoc/>
    public async Task<ApiResp> ResetReportsAsync(string portfolioId, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var endpoint = IPortfolioApiClient.API_REPORT_RESET.Replace("{portfolioId}", portfolioId, StringComparison.OrdinalIgnoreCase);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Post, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<ReportPeriod>>> GetReportPeriodsAsync(string portfolioId, ReportType type, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
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
        return await ReadAndCloseResponseAsync<IEnumerable<ReportPeriod>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<ReportResp>>> GetReportSnapshotAsync(string portfolioId, ReportType type, string periodStart, string symbol, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "pid", portfolioId },
            { "start", periodStart },
            { "symbol", string.IsNullOrEmpty(symbol) ? ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO : symbol.ToUpper() },
        };
        var endpoint = IPortfolioApiClient.API_REPORT_SNAPSHOT.Replace("{type}", type.ToString(), StringComparison.OrdinalIgnoreCase);
        endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<ReportResp>>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<ReportResp>>> GetReportTrendAsync(string portfolioId, ReportType type, string symbol, int count, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> {
            { "pid", portfolioId },
            { "symbol", string.IsNullOrEmpty(symbol) ? ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO : symbol.ToUpper() },
            { "count", count.ToString() },
        };
        var endpoint = IPortfolioApiClient.API_REPORT_TREND.Replace("{type}", type.ToString(), StringComparison.OrdinalIgnoreCase);
        endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, endpoint,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<ReportResp>>(httpResult, cancellationToken);
    }
}
