using System.Text.Json;
using MyPo.Blazor.App.Helpers;
using MyPo.Blazor.App.Services;
using MyPo.Blazor.App.Shared;
using MyPo.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MyPo.Blazor.App.Pages;

public partial class Login : BaseComponent
{
	private CModal ModalDialog { get; set; } = default!;

	private void ShowModalNotImplemented()
	{
		ModalDialog.Open();
	}

	private string Email { get; set; } = string.Empty;
	private string Password { get; set; } = string.Empty;

	private IEnumerable<string> ExternalAuthProviders { get; set; } = [];

	private string AlertType { get; set; } = "info";
	private string AlertMessage { get; set; } = string.Empty;
	private bool HideLoginForm { get; set; } = false;
	private bool DisabledLocalAuth { get; set; } = false;
	private bool DisableExternalLogin { get; set; } = false;

	[Inject]
	private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

	[Inject]
	private IConfiguration AppConfig { get; set; } = default!;

	private async void CloseAlert()
	{
		AlertMessage = string.Empty;
		await InvokeAsync(StateHasChanged);
	}

	private bool alertIntact = true;
	private async void ShowAlert(string type, string message, bool setAlertIntactFlag = false)
	{
		AlertType = type;
		AlertMessage = message;
		alertIntact = setAlertIntactFlag;
		await InvokeAsync(StateHasChanged);
	}

	[Inject]
	private ILogger<Login> Logger { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender && DisabledLocalAuth)
		{
			ShowAlert("warning", "Local authentication is disabled. Please use external login providers.");
			return;
		}

		// FIXME: NOT TO USE THIS IN PRODUCTION!
		// for demo purpose: automatically fill the login form
		// if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
		if (firstRender && HostEnvironment.Equals(EnvironmentName.Development, MyPo.Shared.Globals.StringComparison))
		{
			Logger.LogCritical("DevMode - automatically fill the login form. DO NOT USE THIS IN PRODUCTION!");
			var seedUsers = await ApiClient.GetSeedUsersAsync(ApiBaseUrl);
			Logger.LogCritical("Seed users: {users}", JsonSerializer.Serialize(seedUsers));
			var user = seedUsers.Data?.FirstOrDefault();
			Email = user?.Email ?? string.Empty;
			Password = user?.Password ?? string.Empty;

			ShowAlert("info", "DevMode: Automatically fill login info.");
		}
	}

	protected override async Task OnInitializedAsync()
	{
		ShowAlert("waiting", "Please wait...", setAlertIntactFlag: true);
		await base.OnInitializedAsync();

		DisabledLocalAuth = AppConfig.GetValue<bool>(MyPo.Shared.Globals.CONF_AUTH_DISABLED_LOCAL_AUTH);

		var providers = await ApiClient.GetExternalAuthProvidersAsync(ApiBaseUrl);
		ExternalAuthProviders = providers.Data ?? [];

		if (alertIntact) CloseAlert();
	}

	private async void BtnClickExternalLogin(string provider)
	{
		HideLoginForm = DisableExternalLogin = true;
		ShowAlert("waiting", "Redirecting to external login provider...");
		var returnUrl = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query)
			.TryGetValue("returnUrl", out var returnUrlValue) ? returnUrlValue.FirstOrDefault("/") : "/";

		UriBuilder uriBuilder;
		switch (provider.ToUpper())
		{
			case "LINKEDIN":
				uriBuilder = new UriBuilder(NavigationManager.BaseUri)
				{
					Path = UIGlobals.ROUTE_LOGIN_EXTERNAL_LINKEDIN,
					Query = QueryHelpers.AddQueryString(string.Empty, "returnUrl", returnUrl)
				};
				break;
			case "MICROSOFT":
				uriBuilder = new UriBuilder(NavigationManager.BaseUri)
				{
					Path = UIGlobals.ROUTE_LOGIN_EXTERNAL_MICROSOFT,
					Query = QueryHelpers.AddQueryString(string.Empty, "returnUrl", returnUrl)
				};
				break;
			case "GITHUB":
				uriBuilder = new UriBuilder(NavigationManager.BaseUri)
				{
					Path = UIGlobals.ROUTE_LOGIN_EXTERNAL_GITHUB,
					Query = QueryHelpers.AddQueryString(string.Empty, "returnUrl", returnUrl)
				};
				break;
			default:
				HideLoginForm = DisableExternalLogin = false;
				ShowModalNotImplemented();
				return;
		}
		if ((uriBuilder.Scheme == "http" && uriBuilder.Port == 80) || (uriBuilder.Scheme == "https" && uriBuilder.Port == 443))
		{
			uriBuilder.Port = -1;
		}
		var apiResult = await ApiClient.GetExternalAuthUrlAsync(new ExternalAuthUrlReq()
		{
			Provider = provider,
			RedirectUrl = uriBuilder.ToString()
		}, ApiBaseUrl);
		if (apiResult.Status != 200)
		{
			HideLoginForm = DisableExternalLogin = false;
			ShowAlert("danger", $"{apiResult.Status}: {apiResult.Message ?? "Failed to get external auth URL."}");
			return;
		}

		await Task.Delay(100); // UI hack to have the alert displayed before redirecting
		NavigationManager.NavigateTo(apiResult.Data ?? UIGlobals.ROUTE_HOME);
	}

	private async void BtnClickLogin()
	{
		if (DisabledLocalAuth)
		{
			ShowAlert("danger", "Local authentication is disabled. Please use external login providers.");
			return;
		}
		if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
		{
			ShowAlert("warning", "Please enter Email and Password to login.");
			return;
		}

		HideLoginForm = DisableExternalLogin = true;
		ShowAlert("waiting", "Authenticating, please wait...");
		var req = new AuthReq()
		{
			Email = Email,
			Password = Password
		};
		var resp = await ApiClient.LoginAsync(req, ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideLoginForm = DisableExternalLogin = false;
			ShowAlert("danger", resp.Message!);
			return;
		}

		ShowAlert("success", "Authenticated successfully, logging in...⏳");
		var returnUrl = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query)
			.TryGetValue("returnUrl", out var returnUrlValue) ? returnUrlValue.FirstOrDefault("/") : "/";

		var localStorage = ServiceProvider.GetRequiredService<LocalStorageHelper>();
		await localStorage.SetItemAsync(Globals.LOCAL_STORAGE_KEY_AUTH_TOKEN, resp.Data.Token!);
		((JwtAuthenticationStateProvider)AuthenticationStateProvider).NotifyStageChanged();

		NavigationManager.NavigateTo(returnUrl ?? "/", forceLoad: false);
	}
}
