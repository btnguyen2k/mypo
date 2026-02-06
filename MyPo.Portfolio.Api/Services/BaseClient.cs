using MyPo.Shared.Api;
using System.Text.Json;

namespace MyPo.Portfolio.Api.Services;

public class BaseClient
{
	private readonly HttpClient defaultHttpClient;
	private readonly string baseUrl;

	public BaseClient(HttpClient httpClient, string baseUrl = "")
	{
		defaultHttpClient = httpClient;
		this.baseUrl = baseUrl;
	}

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
		UsingBaseUrlAndHttpClient(baseUrl, requestHttpClient, out var usingBaseUrl, out var usingHttpClient);
		var apiUri = new Uri(new Uri(usingBaseUrl), apiEndpoint);
		using var httpReq = BuildRequest(method, apiUri, authToken, requestData);
		return await usingHttpClient.SendAsync(httpReq, cancellationToken);
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
