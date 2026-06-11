using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPreferences : BasePage
{
	private const string PAGE_TITLE = "My Preferences";

	private const string TabIdMarketAlert = "pref-market-alert";

	/// <summary>
	/// Metadata describing a preference group tab. Add a new entry here (plus a matching
	/// tab-pane in the markup and a self-contained component) to introduce a new group.
	/// </summary>
	private sealed record PreferenceGroup(string Id, string Title);

	private static readonly List<PreferenceGroup> PreferenceGroups =
	[
		new(TabIdMarketAlert, "📡 Market Alerts"),
	];

	private string ActiveTab { get; set; } = TabIdMarketAlert;

	private void SwitchTab(string tab)
	{
		CloseAlert();
		ActiveTab = tab;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			ShowPassedMessageOrCloseAlert();
		}
	}
}
