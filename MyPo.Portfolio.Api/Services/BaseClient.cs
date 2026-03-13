using MyPo.Shared.Api;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Api.Services;

public class BaseClient
{
	private readonly HttpClient defaultHttpClient;
	private readonly string baseUrl;
	private readonly ILogger<BaseClient> logger;

	public BaseClient(ILogger<BaseClient> logger, HttpClient httpClient, string baseUrl = "")
	{
		this.defaultHttpClient = httpClient;
		this.baseUrl = baseUrl;
		this.logger = logger;

		defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
	}

	private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10*60);

	protected void UsingBaseUrlAndHttpClient(string? baseUrl, HttpClient? requestHttpClient, out string usingBaseUrl, out HttpClient usingHttpClient)
	{
		usingBaseUrl = string.IsNullOrEmpty(baseUrl) ? this.baseUrl : baseUrl;
		usingHttpClient = requestHttpClient ?? defaultHttpClient;
	}

	protected static HttpRequestMessage BuildRequest(HttpMethod method, Uri endpoint, string? authToken, object? requestData)
	{
		var req = new HttpRequestMessage(method, endpoint)
		{
			Headers = {
				{ "Accept", "application/json" }
			}
		};
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
				Content = JsonContent.Create(new ApiResp {
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
			return new ApiResp<T> {
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
