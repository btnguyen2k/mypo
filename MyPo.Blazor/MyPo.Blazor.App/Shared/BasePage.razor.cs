using Microsoft.AspNetCore.WebUtilities;

namespace MyPo.Blazor.App.Shared;

/// <summary>
/// Base Razor page common properties and utility methods.
/// </summary>
public abstract class BasePage : BaseComponent
{
	protected string AlertMessage { get; set; } = string.Empty;
	protected string AlertType { get; set; } = "info";
	protected bool AlertHasChanged {get; set; } = false;
	protected bool HideUI { get; set; } = false;

	protected void CloseAlert()
	{
		AlertMessage = string.Empty;
		AlertHasChanged = false;
		StateHasChanged();
	}

	protected void ShowAlert(string type, string message)
	{
		var oldAlertType = AlertType;
		var oldAlertMessage = AlertMessage;
		AlertType = type;
		AlertMessage = message;
		AlertHasChanged = !String.IsNullOrEmpty(oldAlertMessage)
			&& (String.Compare(oldAlertMessage, message, MyPo.Shared.Globals.StringComparison) != 0
				|| String.Compare(oldAlertType, type, MyPo.Shared.Globals.StringComparison) != 0);
		StateHasChanged();
	}

	public const string QUERY_PARM_ALERT_TYPE = "alertType";
	public const string QUERY_PARM_ALERT_MESSAGE = "alertMessage";
	public const string QUERY_PARM_REFRESH = "refresh";

	protected (string, string) GetPassedMessageFromQuery()
	{
		var queryParameters = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
		var alertType = queryParameters.TryGetValue(QUERY_PARM_ALERT_TYPE, out var alertTypeValue) ? alertTypeValue.ToString().Trim() : string.Empty;
		var alertMessage = queryParameters.TryGetValue(QUERY_PARM_ALERT_MESSAGE, out var alertMessageValue) ? alertMessageValue.ToString().Trim() : string.Empty;
		return (alertType, alertMessage);
	}
}
