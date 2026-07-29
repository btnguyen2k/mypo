using Microsoft.Extensions.Logging;
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
        AttachedHeaders = attachedHeaders != null ? new Dictionary<string, string>(attachedHeaders) : new Dictionary<string, string>(DEFAULT_HEADERS);

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
        if (requestData != null)
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
}
