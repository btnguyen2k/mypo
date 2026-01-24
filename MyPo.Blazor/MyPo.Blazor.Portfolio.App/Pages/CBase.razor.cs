using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public abstract class CBase : BaseComponent
{
	protected string AlertType { get; set; } = string.Empty;
	protected string AlertMessage { get; set; } = string.Empty;
	protected bool AlertHasChanged {get; set; } = false;
	protected CAlert Alert { get; set; } = default!;

	protected void CloseAlert()
	{
		Alert.Hide();
		// AlertMessage = string.Empty;
		// AlertHasChanged = false;
		StateHasChanged();
	}

	protected void ShowAlert(string type, string message, int autoCloseAfterMs = 0)
	{
		Alert.Show(type, message, autoCloseAfterMs);
		// var oldAlertType = AlertType;
		// var oldAlertMessage = AlertMessage;
		// AlertType = type;
		// AlertMessage = message;
		// AlertHasChanged = !String.IsNullOrEmpty(oldAlertMessage)
		// 	&& (String.Compare(oldAlertMessage, message, MyPo.Shared.Globals.StringComparison) != 0
		// 		|| String.Compare(oldAlertType, type, MyPo.Shared.Globals.StringComparison) != 0);
		StateHasChanged();
	}
}
