using MyPo.Blazor.App.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.Portfolio.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPreferences : BasePage
{
    private const string PAGE_TITLE = "My Preferences";

    private const string LOCAL_STORAGE_KEY_ACTIVE_TAB = "MyPreferences-active-tab";

    private const string TabIdMarketAlert = "pref-market-alert";

    private const string TabIdPortfolioPlan = "pref-portfolio-plan";

    /// <summary>
    /// Metadata describing a preference group tab. Add a new entry here (plus a matching
    /// tab-pane in the markup and a self-contained component) to introduce a new group.
    /// </summary>
    private sealed record PreferenceGroup(string Id, string Title);

    private static readonly List<PreferenceGroup> PreferenceGroups =
    [
        new(TabIdMarketAlert, "📡 Market Alerts"),
        new(TabIdPortfolioPlan, "📋 Portfolio Plans"),
    ];

    private string ActiveTab { get; set; } = TabIdMarketAlert;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private async void SwitchTab(string tab)
    {
        CloseAlert();
        ActiveTab = tab;
        var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
        await jsLocalStorage.InvokeAsync<object>("LocalStoreSet", LOCAL_STORAGE_KEY_ACTIVE_TAB, tab);
    }

    private async Task SwitchToSavedTab()
    {
        var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
        var savedTab = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", LOCAL_STORAGE_KEY_ACTIVE_TAB);
        if (!string.IsNullOrEmpty(savedTab) && PreferenceGroups.Any(g => g.Id == savedTab))
        {
            ActiveTab = savedTab;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await SwitchToSavedTab();
            ShowPassedMessageOrCloseAlert();
        }
    }
}
