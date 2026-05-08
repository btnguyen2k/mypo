using MyPo.Blazor.App.Helpers;
using MyPo.Blazor.App.Services;
using MyPo.Blazor.App.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace MyPo.Blazor.App.Pages.ExternalAuth;

public partial class LoginLinkedin
{
	protected override async Task OnInitializedAsync()
	{
		ShowAlert("waiting", "Authenticating with LinkedIn, please wait.", "Authenticating...");
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
			Path = UIGlobals.ROUTE_LOGIN_EXTERNAL_LINKEDIN,
		};
		if ((uriBuilder.Scheme == "http" && uriBuilder.Port == 80) || (uriBuilder.Scheme == "https" && uriBuilder.Port == 443))
		{
			uriBuilder.Port = -1;
		}
		authData["redirect_uri"] = uriBuilder.ToString();
		var apiResult = await ApiClient.ExternalLoginAsync(new MyPo.Shared.Api.ExternalAuthReq
		{
			Provider = "LinkedIn",
			AuthData = authData
		}, ApiBaseUrl);
		if (!apiResult.IsSuccess)
		{
			ShowReturnLinks = true;
			ShowAlert("error", $"{apiResult.Status}: {apiResult.Message}", "Error");
			return;
		}

		ShowAlert("wait", "LinkedIn authentication was successful, signing you in, please wait.", "Logging in...");
		ShowReturnLinks = true;
		var localStorage = ServiceProvider.GetRequiredService<LocalStorageHelper>();
		await localStorage.SetItemAsync(Globals.LOCAL_STORAGE_KEY_AUTH_TOKEN, apiResult.Data.Token!);
		((JwtAuthenticationStateProvider)AuthenticationStateProvider).NotifyStageChanged();
		var returnUrl = apiResult.Data.ReturnUrl ?? UIGlobals.ROUTE_HOME;
		NavigationManager.NavigateTo(returnUrl, forceLoad: false);
	}
}
