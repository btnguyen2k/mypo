using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public abstract class CBase : BaseComponent
{
	protected CAlert Alert { get; set; } = new();

	protected void CloseAlert()
	{
		Alert.Hide();
		StateHasChanged();
	}

	protected void ShowAlert(string type, string message, int autoCloseAfterMs = 0)
	{
		Alert.Show(type, message, autoCloseAfterMs);
		StateHasChanged();
	}
}
