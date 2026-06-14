using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;

namespace MyPo.Blazor.Portfolio.App.Pages.Preferences;

public partial class CPortfolioPlanPreferences : CBase
{
    private const int ALERT_AUTO_CLOSE_MS = 15000;

    private bool Saving { get; set; } = false;

    private bool EnablePortfolioPlanAlertsViaTelegrams { get; set; } = false;
    private int AutoUpdateDays { get; set; } = 7;
    private int AutoAnalyzeDays { get; set; } = 2;
    private string TelegramBotApiKey { get; set; } = "";
    private string TelegramChatIDs { get; set; } = "";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        LoadFromCurrentUser();
    }

    private void LoadFromCurrentUser()
    {
        if (CurrentUser == null)
        {
            return;
        }
        var prefs = CurrentUser.Value.Metadata?.GetPortfolioPlanPreferences() ?? new PortfolioPlanPreferences();
        EnablePortfolioPlanAlertsViaTelegrams = prefs.ViaTelegram;
        AutoUpdateDays = prefs.AutoUpdateDays;
        AutoAnalyzeDays = prefs.AutoAnalyzeDays;
        // Telegram secrets are never returned by the API; leave the inputs blank.
        TelegramBotApiKey = "";
        TelegramChatIDs = "";
        CloseAlert();
    }

    private async Task BtnClickSave()
    {
        Saving = true;
        ShowAlert("info", "Saving preferences...");
        var req = new SaveMyPrefPortfolioPlanReq()
        {
            EnablePortfolioPlanAlertsViaTelegrams = EnablePortfolioPlanAlertsViaTelegrams,
            PortfolioPlanAutoUpdateDays = AutoUpdateDays,
            PortfolioPlanAutoAnalyzeDays = AutoAnalyzeDays,
            TelegramBotApiKey = TelegramBotApiKey.Trim(),
            TelegramChatIDs = TelegramChatIDs.Trim()
        };
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var result = await apiClient.SaveMyPreferencesPortfolioPlanAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
        if (result.Status != 200)
        {
            ShowAlert("error", $"Failed to save preferences: {result.Message}");
        }
        else
        {
            ShowAlert("success", "Preferences saved successfully!", ALERT_AUTO_CLOSE_MS);
        }
        Saving = false;
    }
}
