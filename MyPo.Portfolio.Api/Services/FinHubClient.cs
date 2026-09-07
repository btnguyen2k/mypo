using System.Net;
using Finhub.Client;
using FinHub.Client.Schemas;
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
        var apiKey = Environment.GetEnvironmentVariable("FINHUB_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            AddAttachedHeaders(new Dictionary<string, string>
            {
                { "X-Api-Key", apiKey },
            });
        }
    }

    private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10 * 60);
    /// <inheritdoc/>
    protected override void SetupDefaultHttpClient(HttpClient defaultHttpClient)
    {
        base.SetupDefaultHttpClient(defaultHttpClient);
        defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
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

    // private static async Task<ApiResp<T>> SendRequestAndPool<T>(
    //     Func<Task<HttpResponseMessage>> buildAndSendTaskRequest,
    //     Func<string, Task<HttpResponseMessage>> buildAndSendPollRequest,
    //     TimeSpan timeout,
    //     CancellationToken cancellationToken = default)
    // {
    //     var start = DateTimeOffset.Now;

    //     using var httpResultTask = await buildAndSendTaskRequest();
    //     var apiResultTask = await ReadAndCloseResponseAsync<T>(httpResultTask, cancellationToken: cancellationToken);
    //     var taskInfo = apiResultTask.ExtraAs<AsyncTaskInfo>();
    //     var taskId = taskInfo?.TaskId ?? string.Empty;
    //     if (string.IsNullOrEmpty(taskId) && apiResultTask.Status != (int)HttpStatusCode.OK)
    //     {
    //         return new ApiResp<T>()
    //         {
    //             Status = apiResultTask.IsSuccess ? (int)HttpStatusCode.InternalServerError : apiResultTask.Status,
    //             Message = string.IsNullOrEmpty(apiResultTask.Message)
    //                 ? "No task-id returned from server"
    //                 : apiResultTask.Message,
    //         };
    //     }

    //     while (apiResultTask.IsSuccess)
    //     {
    //         if (apiResultTask.Status == (int)HttpStatusCode.OK)
    //         {
    //             return apiResultTask;
    //         }
    //         if (DateTimeOffset.Now - start > timeout)
    //         {
    //             return new ApiResp<T>()
    //             {
    //                 Status = (int)HttpStatusCode.RequestTimeout,
    //                 Message = $"Timeout exceeded {timeout.TotalMilliseconds} ms",
    //             };
    //         }
    //         var delayMs = Random.Shared.Next(5000, 10000); // delay randomly 5-10 secs
    //         await Task.Delay(delayMs, cancellationToken: cancellationToken);
    //         using (var httpResultPoll = await buildAndSendPollRequest(taskId))
    //         {
    //             apiResultTask = await ReadAndCloseResponseAsync<T>(httpResultPoll, cancellationToken: cancellationToken);
    //         }
    //     }

    //     return apiResultTask;
    // }
}
