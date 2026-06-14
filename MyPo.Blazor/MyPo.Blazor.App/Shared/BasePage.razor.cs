using Microsoft.AspNetCore.WebUtilities;
using MyPo.Shared.Api;

namespace MyPo.Blazor.App.Shared;

/// <summary>
/// Base Razor page common properties and utility methods.
/// </summary>
public abstract class BasePage : BaseComponent
{
	protected bool HideUI { get; set; } = false;

	protected string BackgroundMsg { get; set; } = string.Empty;

	protected UserResp? CurrentUser { get; set; }

	protected void SetBackgroundMsg(string msg)
	{
		BackgroundMsg = msg;
		StateHasChanged();
	}

	protected void ClearBackgroundMsg()
	{
		BackgroundMsg = string.Empty;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		var userResp = await ApiClient.GetMyInfoAsync(await GetAuthTokenAsync(), ApiBaseUrl);
		CurrentUser = userResp.Status == 200 ? userResp.Data : null;
	}

	protected const int ALERT_AUTO_CLOSE_MS = 15000;
	protected CAlert? Alert { get; set; } = null;

	protected void CloseAlert()
	{
		if (Alert == null) return;
		Alert.Hide();
		StateHasChanged();
	}

	public void ShowAlert(string type, string message, int autoCloseAfterMs = 0)
	{
		if (Alert == null) return;
		Alert.Show(type, message, autoCloseAfterMs);
		StateHasChanged();
	}

	public const string QUERY_PARM_ALERT_TYPE = "alertType";
	public const string QUERY_PARM_ALERT_MESSAGE = "alertMessage";
	public const string QUERY_PARM_REFRESH = "refresh";

	protected bool RemoveRefreshParamIfPresent()
	{
		var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
		if (queryParams.AllKeys.Contains(QUERY_PARM_REFRESH))
		{
			// remove the refresh query parameter
			queryParams.Remove(QUERY_PARM_REFRESH);
			var uriBuilder = new UriBuilder(NavigationManager.ToAbsoluteUri(NavigationManager.Uri))
			{
				Query = queryParams.ToString() ?? string.Empty
			};
			NavigationManager.NavigateTo(uriBuilder.Uri.ToString(), forceLoad: false);
			return true;
		}
		return false;
	}

	protected (string, string) GetPassedMessageFromQuery()
	{
		var queryParameters = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
		var alertType = queryParameters.TryGetValue(QUERY_PARM_ALERT_TYPE, out var alertTypeValue) ? alertTypeValue.ToString().Trim() : string.Empty;
		var alertMessage = queryParameters.TryGetValue(QUERY_PARM_ALERT_MESSAGE, out var alertMessageValue) ? alertMessageValue.ToString().Trim() : string.Empty;
		return (alertType, alertMessage);
	}

	protected void ShowPassedMessageOrCloseAlert()
	{
		var (alertType, alertMessage) = GetPassedMessageFromQuery();
		if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
		{
			var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
			// remove the alert query parameters to prevent showing the same message again on next page load/refresh
			queryParams.Remove(QUERY_PARM_ALERT_TYPE);
			queryParams.Remove(QUERY_PARM_ALERT_MESSAGE);
			var uriBuilder = new UriBuilder(NavigationManager.ToAbsoluteUri(NavigationManager.Uri))
			{
				Query = queryParams.ToString() ?? string.Empty
			};
			NavigationManager.NavigateTo(uriBuilder.Uri.ToString(), forceLoad: false);

			ShowAlert(alertType, alertMessage, autoCloseAfterMs: ALERT_AUTO_CLOSE_MS);
		}
		else
		{
			CloseAlert();
		}
	}
}
