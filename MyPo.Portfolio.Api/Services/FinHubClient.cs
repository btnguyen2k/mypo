using System.Net;
using Finhub.Client;
using FinHub.Client.Schemas;
using Microsoft.AspNetCore.WebUtilities;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseApiClient, IFinHubClient
{
    public FinHubClient(
        HttpClient httpClient,
        string baseUrl = "",
        IDictionary<string, string>? attachedHeaders = null,
        ILogger<FinHubClient>? logger = null) : base(httpClient, baseUrl, attachedHeaders, logger)
    {
        var myAttachedHeaders = new Dictionary<string, string>();

        var apiKey = Environment.GetEnvironmentVariable("FINHUB_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            myAttachedHeaders["X-Api-Key"] = apiKey;
        }

        AddAttachedHeaders(myAttachedHeaders);
    }

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

    private async Task<TAsyncResponse> StartApiRequestAsync<TAsyncResponse, TData>(string endpoint, object reqData, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
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
        object reqData,
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

    private static async Task<T> SendApiRequestAndPoll<T>(
        Func<Task<HttpResponseMessage>> buildAndSendTaskRequest,
        Func<string, Task<HttpResponseMessage>> buildAndSendPollRequest,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)  where T : ApiResp, new()
    {
        var timerStart = DateTimeOffset.Now;
        using var httpResultTask = await buildAndSendTaskRequest();
        var apiResultTask = await ReadAndCloseResponseAsApiRespAsync<T>(httpResultTask, cancellationToken: cancellationToken);
        while (apiResultTask.IsSuccess)
        {
            var taskInfo = apiResultTask.ExtraAs<AsyncTaskInfo>();
            switch (taskInfo?.State)
            {
                case TaskState.Completed:
                    return apiResultTask;
                case TaskState.Failed:
                    return new T()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiResultTask.Message)
                            ? $"Task '{taskInfo.TaskId}' failed."
                            : apiResultTask.Message,
                    };
                case TaskState.Running:
                    if (DateTimeOffset.Now - timerStart > timeout)
                    {
                        return new T()
                        {
                            Status = (int)HttpStatusCode.RequestTimeout,
                            Message = $"Task '{taskInfo.TaskId}' timeout exceeded {timeout.TotalMilliseconds} ms",
                        };
                    }
                    var delayMs = Random.Shared.Next(5000, 10000); // delay randomly 5-10 secs
                    await Task.Delay(delayMs, cancellationToken: cancellationToken);
                    using (var httpResultPoll = await buildAndSendPollRequest(taskInfo.TaskId))
                    {
                        apiResultTask = await ReadAndCloseResponseAsApiRespAsync<T>(httpResultPoll, cancellationToken: cancellationToken);
                    }
                    break;
                default:
                    return new T()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiResultTask.Message)
                            ? $"Task '{taskInfo?.TaskId}' has unknown state '{taskInfo?.State}'."
                            : apiResultTask.Message,
                    };
            }
        }
        return new T()
        {
            Status = apiResultTask.IsSuccess ? (int)HttpStatusCode.InternalServerError : apiResultTask.Status,
            Message = string.IsNullOrEmpty(apiResultTask.Message)
                ? "Error occurred while sending task request to server"
                : apiResultTask.Message,
        };
    }
}
