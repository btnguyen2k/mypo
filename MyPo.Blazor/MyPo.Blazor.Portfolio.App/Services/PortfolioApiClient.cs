using System.Net;
using FinHub.Client.Schemas;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Services;

public partial class PortfolioApiClient : BaseApiClient, IPortfolioApiClient
{
    public PortfolioApiClient(
        HttpClient httpClient,
        string baseUrl = "",
        IDictionary<string, string>? attachedHeaders = null,
        ILogger<PortfolioApiClient>? logger = null) : base(httpClient, baseUrl, attachedHeaders, logger) { }

    private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10 * 60);
    /// <inheritdoc/>
    protected override void SetupDefaultHttpClient(HttpClient defaultHttpClient)
    {
        base.SetupDefaultHttpClient(defaultHttpClient);
        defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
    }

    private static string AddTaskIdToEndpoint(string endpoint, string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
        return QueryHelpers.AddQueryString(endpoint, queryParams);
    }

    private async Task<TAsyncResponse> StartApiRequestAsync<TAsyncResponse, TData>(
        string endpoint,
        object? reqData,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
    {
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpoint,
            NoAuth,
            reqData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsApiRespAsync<TAsyncResponse>(httpResult, cancellationToken: cancellationToken);
    }

    private async Task<TAsyncResponse> PollApiResultAsync<TAsyncResponse, TData>(string taskId, string endpoint, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
    {
        var endpointPoll = AddTaskIdToEndpoint(endpoint, taskId);
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            HttpMethod.Post, baseUrl, endpointPoll,
            NoAuth,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsApiRespAsync<TAsyncResponse>(httpResult, cancellationToken: cancellationToken);
    }

    private async Task<TResponse> SendApiRequestAndPollAsync<TAsyncResponse, TResponse, TData>(
        string endpoint,
        object? reqData,
        TimeSpan timeout,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
        where TResponse : ApiResp<TData>, new()
    {
        var timerStart = DateTimeOffset.Now;
        timeout = timeout >= MIN_TIMEOUT ? timeout : MIN_TIMEOUT;
        var apiRequestResult = await StartApiRequestAsync<TAsyncResponse, TData>(endpoint, reqData, baseUrl, httpClient, cancellationToken);
        while (apiRequestResult.IsSuccess)
        {
            var taskInfo = apiRequestResult.Extra;
            switch (taskInfo?.State)
            {
                case TaskState.Completed:
                    return apiRequestResult.ToApiResp<TResponse>();
                case TaskState.Failed:
                    return new TResponse()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiRequestResult.Message)
                            ? $"Task '{taskInfo.TaskId}' failed."
                            : apiRequestResult.Message,
                    };
                case TaskState.Running:
                    if (DateTimeOffset.Now - timerStart > timeout || cancellationToken.IsCancellationRequested)
                    {
                        return new TResponse()
                        {
                            Status = (int)HttpStatusCode.RequestTimeout,
                            Message = cancellationToken.IsCancellationRequested
                                ? $"Task '{taskInfo.TaskId}' has been canceled."
                                : $"Task '{taskInfo.TaskId}' timeout exceeded {timeout.TotalMilliseconds} ms.",
                        };
                    }
                    var delayMs = Random.Shared.Next(5000, 10000); // delay randomly 5-10 secs
                    await Task.Delay(delayMs, cancellationToken: cancellationToken);
                    var taskId = taskInfo.TaskId;
                    apiRequestResult = await PollApiResultAsync<TAsyncResponse, TData>(taskId, endpoint, baseUrl, httpClient, cancellationToken);
                    break;
                default:
                    return new TResponse()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiRequestResult.Message)
                            ? $"Task '{taskInfo?.TaskId}' has unknown state '{taskInfo?.State}'."
                            : apiRequestResult.Message,
                    };
            }
        }
        return new TResponse()
        {
            Status = apiRequestResult.IsSuccess ? (int)HttpStatusCode.InternalServerError : apiRequestResult.Status,
            Message = string.IsNullOrEmpty(apiRequestResult.Message)
                ? "Error occurred while sending task request to server"
                : apiRequestResult.Message,
        };
    }

    /*----------------------------------------------------------------------*/

    /// <inheritdoc/>
    public async Task<ApiResp<string[]>> DebugAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, IPortfolioApiClient.API_DEBUG,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<string[]>(httpResult, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResp<IEnumerable<MarketDefResp>>> GetMarketsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
    {
        using var httpResult = await BuildAndSendRequestAsync(
            requestHttpClient,
            HttpMethod.Get, baseUrl, IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MARKETS,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsync<IEnumerable<MarketDefResp>>(httpResult, cancellationToken);
    }
}
