using Microsoft.Extensions.Logging;
using MyPo.Shared.Api;

namespace MyPo.Blazor.App.Services;

public class ApiClient : BaseApiClient, IApiClient
{
    public ApiClient(
        HttpClient httpClient,
        string baseUrl = "",
        IDictionary<string, string>? attachedHeaders = null,
        ILogger<ApiClient>? logger = null) : base(httpClient, baseUrl, attachedHeaders, logger) { }

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<InfoResp>> InfoAsync(string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_INFO,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<InfoResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<AuthResp>> LoginAsync(AuthReq req, string? baseUrl = null, HttpClient? requestHttpClient = null, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_AUTH_SIGNIN,
			NoAuth,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<AuthResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<AuthResp>> RefreshAsync(string authToken, string? baseUrl = null, HttpClient? requestHttpClient = null, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_AUTH_REFRESH,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<AuthResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> GetMyInfoAsync(string authToken, string? baseUrl = null, HttpClient? requestHttpClient = null, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_USERS_ME,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> UpdateMyProfileAsync(UpdateUserProfileReq req, string authToken, string? baseUrl = null, HttpClient? requestHttpClient = null, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_USERS_ME_PROFILE,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<ChangePasswordResp>> ChangeMyPasswordAsync(ChangePasswordReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_USERS_ME_PASSWORD,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<ChangePasswordResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<UserResp>>> GetAllUsersAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_USERS,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<UserResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> CreateUserAsync(CreateOrUpdateUserReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_USERS,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> GetUserAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_USERS_ID.Replace("{id}", id),
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> DeleteUserAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, IApiClient.API_ENDPOINT_USERS_ID.Replace("{id}", id),
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<UserResp>> UpdateUserAsync(string id, CreateOrUpdateUserReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, IApiClient.API_ENDPOINT_USERS_ID.Replace("{id}", id),
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<UserResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<ClaimResp>>> GetAllClaimsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_CLAIMS,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<ClaimResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<RoleResp>>> GetAllRolesAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_ROLES,
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<RoleResp>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<RoleResp>> CreateRoleAsync(CreateOrUpdateRoleReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_ROLES,
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoleResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<RoleResp>> GetRoleAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_ROLES_ID.Replace("{id}", id),
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoleResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<RoleResp>> DeleteRoleAsync(string id, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Delete, baseUrl, IApiClient.API_ENDPOINT_ROLES_ID.Replace("{id}", id),
			authToken,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoleResp>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<RoleResp>> UpdateRoleAsync(string id, CreateOrUpdateRoleReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Put, baseUrl, IApiClient.API_ENDPOINT_ROLES_ID.Replace("{id}", id),
			authToken,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<RoleResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/// <inheritdoc/>
	public async Task<ApiResp<IEnumerable<string>>> GetExternalAuthProvidersAsync(string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Get, baseUrl, IApiClient.API_ENDPOINT_EXTERNAL_AUTH_PROVIDERS,
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<string>>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<string>> GetExternalAuthUrlAsync(ExternalAuthUrlReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_EXTERNAL_AUTH_URL,
			NoAuth,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<string>(httpResult, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiResp<ExternalAuthResp>> ExternalLoginAsync(ExternalAuthReq req, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			httpClient,
			HttpMethod.Post, baseUrl, IApiClient.API_ENDPOINT_EXTERNAL_AUTH_LOGIN,
			NoAuth,
			req,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<ExternalAuthResp>(httpResult, cancellationToken);
	}

	/*----------------------------------------------------------------------*/

	/* FOR DEMO PURPOSES ONLY! */
	public async Task<ApiResp<IEnumerable<UserResp>>> GetSeedUsersAsync(string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default)
	{
		using var httpResult = await BuildAndSendRequestAsync(
			requestHttpClient,
			HttpMethod.Get, baseUrl, "/api/demo/seed_users",
			NoAuth,
			NoData,
			cancellationToken
		);
		return await ReadAndCloseResponseAsync<IEnumerable<UserResp>>(httpResult, cancellationToken);
	}
}
