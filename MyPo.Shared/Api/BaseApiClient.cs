using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyPo.Shared.Api;

public abstract class BaseApiClient
{
	protected readonly HttpClient DefaultHttpClient;
    protected readonly string BaseUrl;
    protected readonly ILogger<BaseApiClient>? Logger;

    private static readonly IDictionary<string, string> DEFAULT_HEADERS = new Dictionary<string, string>
    {
        { "Accept", "application/json" },
    };
    /// <summary>
    /// HTTP request headers applied to every request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected IDictionary<string, string> AttachedHeaders { get; private set; }

	public BaseApiClient(HttpClient httpClient, string baseUrl = "", IDictionary<string, string>? attachedHeaders = null, ILogger<BaseApiClient>? logger = null)
	{
		DefaultHttpClient = httpClient;
        Logger = logger;
        BaseUrl = baseUrl;
        AttachedHeaders = attachedHeaders is not null ? new Dictionary<string, string>(attachedHeaders) : new Dictionary<string, string>(DEFAULT_HEADERS);

        SetupDefaultHttpClient(DefaultHttpClient);
	}

    protected virtual void SetupDefaultHttpClient(HttpClient defaultHttpClient)
    {
        // NOOP
    }

    /// <summary>
    /// Sets the HTTP request headers applied to every request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected void SetAttachedHeaders(IDictionary<string, string>? attachedHeaders)
    {
        AttachedHeaders = attachedHeaders != null ? new Dictionary<string, string>(attachedHeaders) : new Dictionary<string, string>(DEFAULT_HEADERS);
    }

    /// <summary>
    /// Adds (or overwrites) the supplied headers into the HTTP request headers applied to every request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected void AddAttachedHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            AttachedHeaders[header.Key] = header.Value;
        }
    }

	protected virtual void UsingBaseUrlAndHttpClient(string? baseUrl, HttpClient? requestHttpClient, out string usingBaseUrl, out HttpClient usingHttpClient)
	{
		// usingBaseUrl = string.IsNullOrEmpty(baseUrl) ? (Globals.ApiBaseUrl ?? string.Empty) : baseUrl;
        usingBaseUrl = string.IsNullOrEmpty(baseUrl) ? BaseUrl : baseUrl;
		usingHttpClient = requestHttpClient ?? DefaultHttpClient;
	}

    protected virtual HttpRequestMessage BuildRequest(HttpMethod method, Uri endpoint, string? authToken, object? requestData)
    {
        var req = new HttpRequestMessage(method, endpoint);
        foreach (var header in AttachedHeaders)
        {
            req.Headers.Add(header.Key, header.Value);
        }
        if (!string.IsNullOrEmpty(authToken))
        {
            req.Headers.Add("Authorization", $"Bearer {authToken}");
        }
        if (requestData is not null)
        {
            req.Content = JsonContent.Create(requestData);
        }
        return req;
    }

	protected static readonly string NoAuth = string.Empty;
	protected static readonly object? NoData = null;

	protected virtual async Task<HttpResponseMessage> BuildAndSendRequestAsync(HttpClient? requestHttpClient, HttpMethod method, string? baseUrl, string apiEndpoint, string? authToken, object? requestData, CancellationToken cancellationToken)
	{
		UsingBaseUrlAndHttpClient(baseUrl, requestHttpClient, out var usingBaseUrl, out var usingHttpClient);
		var apiUri = new Uri(new Uri(usingBaseUrl), apiEndpoint);
		using var httpReq = BuildRequest(method, apiUri, authToken, requestData);
		return await usingHttpClient.SendAsync(httpReq, cancellationToken);
	}

	protected static async Task<ApiResp> ReadAndCloseResponseAsync(HttpResponseMessage httpResult, CancellationToken cancellationToken)
	{
		try
		{
			var result = await httpResult.Content.ReadFromJsonAsync<ApiResp>(cancellationToken);
			if (result == null)
			{
				return new ApiResp { Status = 500, Message = "Invalid response from server." };
			}
			return result;
		}
		catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OperationCanceledException)
		{
			return new ApiResp { Status = 500, Message = ex.Message };
		}
	}

	protected static async Task<ApiResp<T>> ReadAndCloseResponseAsync<T>(HttpResponseMessage httpResult, CancellationToken cancellationToken)
	{
		try
		{
			var result = await httpResult.Content.ReadFromJsonAsync<ApiResp<T>>(cancellationToken);
			if (result == null)
			{
				return new ApiResp<T> { Status = 500, Message = "Invalid response from server." };
			}
			return result;
		}
		catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OperationCanceledException)
		{
			return new ApiResp<T> { Status = 500, Message = ex.Message };
		}
	}

    protected static async Task<T> ReadAndCloseResponseAsApiRespAsync<T>(HttpResponseMessage httpResult, CancellationToken cancellationToken) where T : ApiResp, new()
	{
		try
		{
			var result = await httpResult.Content.ReadFromJsonAsync<T>(cancellationToken);
			if (result == null)
			{
				return new T { Status = 500, Message = "Invalid response from server." };
			}
			return result;
		}
		catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OperationCanceledException)
		{
			return new T { Status = 500, Message = ex.Message };
		}
	}

    /// <summary>
    /// Sends an API request and polls for the result until completion or timeout.
    /// </summary>
    /// <typeparam name="TAsyncResponse"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TData"></typeparam>
    /// <param name="methodStart">The HTTP method for the initial API request.</param>
    /// <param name="endpointStart">The endpoint URL for the initial API request.</param>
    /// <param name="reqData">The request data for the initial API request.</param>
    /// <param name="methodPoll">The HTTP method for polling the API request status.</param>
    /// <param name="endpointPollWithoutTaskId">The endpoint URL for polling the API request status, without the task ID.</param>
    /// <param name="timeout">The maximum duration to wait for the API request to complete.</param>
    /// <param name="authToken">The authentication token for the API request.</param>
    /// <param name="baseUrl">The base URL for the API request.</param>
    /// <param name="httpClient">The HTTP client to use for the API request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the API request.</param>
    /// <returns>The final API response after polling for completion or timeout.</returns>
    protected virtual async Task<TResponse> SendApiRequestAndPollAsync<TAsyncResponse, TResponse, TData>(
        HttpMethod methodStart,
        string endpointStart,
        object? reqData,
        HttpMethod methodPoll,
        string endpointPollWithoutTaskId,
        TimeSpan timeout,
        string? authToken,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
        where TResponse : ApiResp<TData>, new()
    {
        var timerStart = DateTimeOffset.Now;
        var apiResult = await StartApiRequestAsync<TAsyncResponse, TData>(
            methodStart, endpointStart, reqData,
            authToken,
            baseUrl,
            httpClient,
            cancellationToken);
        while (apiResult.IsSuccess)
        {
            var taskInfo = apiResult.Extra;
            switch (taskInfo?.State)
            {
                case TaskState.Completed:
                    return apiResult.ToApiResp<TResponse>();
                case TaskState.Failed:
                    return new TResponse()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiResult.Message)
                            ? $"Task '{taskInfo.TaskId}' failed."
                            : apiResult.Message,
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
                    var delayMs = Random.Shared.Next(1000, 3000); // delay randomly 1-3 secs
                    await Task.Delay(delayMs, cancellationToken: cancellationToken);
                    var taskId = taskInfo.TaskId;
                    apiResult = await PollApiResultAsync<TAsyncResponse, TData>(
                        methodPoll, endpointPollWithoutTaskId, taskId,
                        authToken,
                        baseUrl,
                        httpClient,
                        cancellationToken);
                    break;
                default:
                    return new TResponse()
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Message = string.IsNullOrEmpty(apiResult.Message)
                            ? $"Task '{taskInfo?.TaskId}' has unknown state '{taskInfo?.State}'."
                            : apiResult.Message,
                    };
            }
        }
        return new TResponse()
        {
            Status = apiResult.IsSuccess ? (int)HttpStatusCode.InternalServerError : apiResult.Status,
            Message = string.IsNullOrEmpty(apiResult.Message)
                ? "Error occurred while sending task request to server"
                : apiResult.Message,
        };
    }

    /// <summary>
    /// Adds the task ID as a query parameter to the given endpoint URL.
    /// </summary>
    /// <param name="endpoint">The endpoint URL to which the task ID should be added.</param>
    /// <param name="taskId">The task ID to add as a query parameter.</param>
    /// <returns>The endpoint URL with the task ID added as a query parameter.</returns>
    protected virtual string AddTaskIdToEndpoint(string endpoint, string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        var queryParams = new Dictionary<string, string?> { { "task_id", taskId } };
        return QueryHelpers.AddQueryString(endpoint, queryParams);
    }

    /// <summary>
    /// Starts an async API request with the specified parameters and returns the asynchronous response.
    /// </summary>
    /// <typeparam name="TAsyncResponse">The type of the asynchronous API response.</typeparam>
    /// <typeparam name="TData">The type of the data contained in the API response.</typeparam>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="reqData">The request data to send with the API request.</param>
    /// <param name="endpointStart">The endpoint URL for the API request.</param>
    /// <param name="authToken">The authentication token to include in the request headers.</param>
    /// <param name="baseUrl">The base URL for the API request.</param>
    /// <param name="httpClient">The HTTP client to use for sending the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the request.</param>
    /// <returns>The asynchronous API response of type <typeparamref name="TAsyncResponse"/>.</returns>
    protected virtual async Task<TAsyncResponse> StartApiRequestAsync<TAsyncResponse, TData>(
        HttpMethod method,
        string endpointStart,
        object? reqData,
        string? authToken,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
    {
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            method, baseUrl, endpointStart,
            authToken,
            reqData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsApiRespAsync<TAsyncResponse>(httpResult, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Polls the API for the result of an asynchronous request using the specified task ID.
    /// </summary>
    /// <typeparam name="TAsyncResponse">The type of the asynchronous API response.</typeparam>
    /// <typeparam name="TData">The type of the data contained in the API response.</typeparam>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="endpointPollWithTaskId">The endpoint URL for polling the API with the task ID.</param>
    /// <param name="authToken">The authentication token to include in the request headers.</param>
    /// <param name="baseUrl">The base URL for the API request.</param>
    /// <param name="httpClient">The HTTP client to use for sending the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the request.</param>
    /// <returns>The asynchronous API response of type <typeparamref name="TAsyncResponse"/>.</returns>
    protected virtual async Task<TAsyncResponse> PollApiResultAsync<TAsyncResponse, TData>(
        HttpMethod method,
        string endpointPollWithTaskId,
        string? authToken,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
    {
        using var httpResult = await BuildAndSendRequestAsync(
            httpClient,
            method, baseUrl, endpointPollWithTaskId,
            authToken,
            NoData,
            cancellationToken
        );
        return await ReadAndCloseResponseAsApiRespAsync<TAsyncResponse>(httpResult, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Polls the API for the result of an asynchronous request using the specified task ID, adding the task ID to the endpoint URL.
    /// </summary>
    /// <typeparam name="TAsyncResponse">The type of the asynchronous API response.</typeparam>
    /// <typeparam name="TData">The type of the data contained in the API response.</typeparam>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="taskId">The task ID to include in the endpoint URL.</param>
    /// <param name="endpointPollWithoutTaskId">The endpoint URL for polling the API without the task ID.</param>
    /// <param name="authToken">The authentication token to include in the request headers.</param>
    /// <param name="baseUrl">The base URL for the API request.</param>
    /// <param name="httpClient">The HTTP client to use for sending the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the request.</param>
    /// <returns>The asynchronous API response of type <typeparamref name="TAsyncResponse"/>.</returns>
    protected virtual async Task<TAsyncResponse> PollApiResultAsync<TAsyncResponse, TData>(
        HttpMethod method,
        string endpointPollWithoutTaskId,
        string taskId,
        string? authToken,
        string? baseUrl = default,
        HttpClient? httpClient = default,
        CancellationToken cancellationToken = default)
        where TAsyncResponse : AsyncApiResponse<TData>, new()
    {
        return await PollApiResultAsync<TAsyncResponse, TData>(
            method,
            AddTaskIdToEndpoint(endpointPollWithoutTaskId, taskId),
            authToken,
            baseUrl,
            httpClient,
            cancellationToken
        );
    }
}
