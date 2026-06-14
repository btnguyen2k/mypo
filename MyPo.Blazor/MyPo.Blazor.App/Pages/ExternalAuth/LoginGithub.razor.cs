using MyPo.Blazor.App.Helpers;
using MyPo.Blazor.App.Services;
using MyPo.Blazor.App.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace MyPo.Blazor.App.Pages.ExternalAuth;

public partial class LoginGithub
{
	protected override async Task OnInitializedAsync()
	{
		ShowAlert("waiting", "Authenticating with GitHub, please wait.", "Authenticating...");
		await base.OnInitializedAsync();

		var authData = ParseQueryParams(htmlDecode: true);
		if (authData.TryGetValue("error", out var errorCode))
		{
			ShowReturnLinks = true;
			ShowAlert("error", $"{errorCode}: {authData["error_description"] ?? string.Empty}", "Error");
			return;
		}

		var uriBuilder = new UriBuilder(NavigationManager.BaseUri)
		{
			Path = UIGlobals.ROUTE_LOGIN_EXTERNAL_GITHUB,
		};
		authData["redirect_uri"] = uriBuilder.ToString();
		var apiResult = await ApiClient.ExternalLoginAsync(new MyPo.Shared.Api.ExternalAuthReq
		{
			Provider = "GitHub",
			AuthData = authData
		}, ApiBaseUrl);
		if (!apiResult.IsSuccess)
		{
			ShowReturnLinks = true;
			ShowAlert("error", $"{apiResult.Status}: {apiResult.Message}", "Error");
			return;
		}

		ShowAlert("wait", "GitHub authentication was successful, signing you in, please wait.", "Logging in...");
		ShowReturnLinks = true;
		var localStorage = ServiceProvider.GetRequiredService<LocalStorageHelper>();
		await localStorage.SetItemAsync(Globals.LOCAL_STORAGE_KEY_AUTH_TOKEN, apiResult.Data.Token!);
		((JwtAuthenticationStateProvider)AuthenticationStateProvider).NotifyStageChanged();
		var returnUrl = apiResult.Data.ReturnUrl ?? UIGlobals.ROUTE_HOME;
		NavigationManager.NavigateTo(returnUrl, forceLoad: false);
	}
}
