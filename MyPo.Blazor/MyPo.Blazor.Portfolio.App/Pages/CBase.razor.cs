using MyPo.Blazor.App.Shared;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public abstract class CBase : BaseComponent
{
    protected UserResp? CurrentUser { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var userResp = await ApiClient.GetMyInfoAsync(await GetAuthTokenAsync(), ApiBaseUrl);
        CurrentUser = userResp.Status == 200 ? userResp.Data : null;
    }

    protected CAlert Alert { get; set; } = new();

    protected async void CloseAlert()
    {
        Alert.Hide();
        await InvokeAsync(StateHasChanged);
    }

    protected async void ShowAlert(string type, string message, int autoCloseAfterMs = 0)
    {
        Alert.Show(type, message, autoCloseAfterMs);
        await InvokeAsync(StateHasChanged);
    }
}
