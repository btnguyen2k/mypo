using MyPo.Shared.Api;
using MyPo.Shared.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyPo.Portfolio.Api.Services;

public class BaseClient
{
    private readonly HttpClient defaultHttpClient;
    private readonly string baseUrl;
    private readonly ILogger<BaseClient> logger;

    private static readonly IDictionary<string, string> DEFAULT_HEADERS = new Dictionary<string, string>
    {
        { "Accept", "application/json" },
    };

    /// <summary>
    /// Default HTTP request headers applied to every request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected IDictionary<string, string> DefaultHeaders { get; private set; }

    public BaseClient(ILogger<BaseClient> logger, HttpClient httpClient, string baseUrl = "", IDictionary<string, string>? defaultHeaders = null)
    {
        this.defaultHttpClient = httpClient;
        this.baseUrl = baseUrl;
        this.logger = logger;
        this.DefaultHeaders = defaultHeaders != null ? new Dictionary<string, string>(defaultHeaders) : new Dictionary<string, string>(DEFAULT_HEADERS);

        defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
    }

    private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10 * 60);

    /// <summary>
    /// Sets the default HTTP request headers applied to every request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected void SetDefaultHeaders(IDictionary<string, string>? defaultHeaders)
    {
        DefaultHeaders = defaultHeaders != null ? new Dictionary<string, string>(defaultHeaders) : new Dictionary<string, string>(DEFAULT_HEADERS);
    }

    /// <summary>
    /// Adds (or overwrites) the supplied headers into the default HTTP request headers applied to every
    /// request built by <see cref="BuildRequest"/>.
    /// </summary>
    protected void AddDefaultHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            DefaultHeaders[header.Key] = header.Value;
        }
    }

    protected void UsingBaseUrlAndHttpClient(string? baseUrl, HttpClient? requestHttpClient, out string usingBaseUrl, out HttpClient usingHttpClient)
    {
        usingBaseUrl = string.IsNullOrEmpty(baseUrl) ? this.baseUrl : baseUrl;
        usingHttpClient = requestHttpClient ?? defaultHttpClient;
    }

    protected HttpRequestMessage BuildRequest(HttpMethod method, Uri endpoint, string? authToken, object? requestData)
    {
        var req = new HttpRequestMessage(method, endpoint);
        foreach (var header in DefaultHeaders)
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

    protected async Task<HttpResponseMessage> BuildAndSendRequestAsync(HttpClient? requestHttpClient, HttpMethod method, string? baseUrl, string apiEndpoint, string? authToken, object? requestData, CancellationToken cancellationToken)
    {
        try
        {
            UsingBaseUrlAndHttpClient(baseUrl, requestHttpClient, out var usingBaseUrl, out var usingHttpClient);
            var apiUri = new Uri(new Uri(usingBaseUrl), apiEndpoint);
            using var httpReq = BuildRequest(method, apiUri, authToken, requestData);
            return await usingHttpClient.SendAsync(httpReq, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while sending request to {apiEndpoint}", apiEndpoint);
            var errorResp = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                Content = JsonContent.Create(new ApiResp
                {
                    Status = 500,
                    Message = "Exception while sending request to API endpoint.",
                    Extras = new Dictionary<string, string> {
                        { "ExceptionType", e.GetType().FullName ?? "UnknownException" },
                        { "ExceptionMessage", e.Message },
                    },
                })
            };
            return errorResp;
        }
    }

    private static readonly JsonSerializerOptions defaultJsonOptions = new()
    {
        Converters = {
            new DefaultDecimalConverter(),
            new DefaultIntConverter(),
            new DefaultLongConverter(),
        },
    };

    protected async Task<ApiResp<T>> ReadAndCloseResponseAsync<T>(HttpResponseMessage httpResult, CancellationToken cancellationToken)
    {
        try
        {
            var result = await httpResult.Content.ReadFromJsonAsync<ApiResp<T>>(defaultJsonOptions, cancellationToken);
            if (result == null)
            {
                return new ApiResp<T> { Status = 500, Message = "Invalid response from server." };
            }
            return result;
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OperationCanceledException)
        {
            var errorMsg = "Exception while reading response content";
            if (ex is JsonException)
            {
                errorMsg = "JSON deserialization error while reading response content";
            }
            logger.LogError(ex, "{errorMsg}", errorMsg);
            return new ApiResp<T>
            {
                Status = 500,
                Message = errorMsg,
                Extras = new Dictionary<string, string> {
                    { "ExceptionType", ex.GetType().FullName ?? "UnknownException" },
                    { "ExceptionMessage", ex.Message },
                    { "StatusCode", ((int)httpResult.StatusCode).ToString() },
                },
            };
        }
    }
}
