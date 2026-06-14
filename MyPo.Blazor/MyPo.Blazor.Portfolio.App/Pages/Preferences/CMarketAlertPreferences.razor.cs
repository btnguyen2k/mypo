using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;

namespace MyPo.Blazor.Portfolio.App.Pages.Preferences;

public partial class CMarketAlertPreferences : CBase
{
    private const int ALERT_AUTO_CLOSE_MS = 15000;

    private bool Saving { get; set; } = false;

    private bool EnableMarketAlertsViaTelegrams { get; set; } = false;
    private string TimeZone { get; set; } = "";
    private int MarketAlertDelayMinutes { get; set; } = 60;
    private TimeOnly StartTime { get; set; } = new TimeOnly(8, 0);
    private TimeOnly EndTime { get; set; } = new TimeOnly(20, 0);
    private bool EnableDayMon { get; set; } = false;
    private bool EnableDayTue { get; set; } = false;
    private bool EnableDayWed { get; set; } = false;
    private bool EnableDayThu { get; set; } = false;
    private bool EnableDayFri { get; set; } = false;
    private bool EnableDaySat { get; set; } = false;
    private bool EnableDaySun { get; set; } = false;
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
        var prefs = CurrentUser.Value.Metadata?.GetMarketAlertPreferences() ?? new MarketAlertPreferences();
        EnableMarketAlertsViaTelegrams = prefs.ViaTelegram;
        TimeZone = prefs.Timezone;
        MarketAlertDelayMinutes = prefs.DelayMinutes;
        StartTime = prefs.StartTime ?? new TimeOnly(8, 0);
        EndTime = prefs.EndTime ?? new TimeOnly(20, 0);
        var daysOfWeek = prefs.DaysOfWeek;
        EnableDayMon = daysOfWeek.Contains("Monday") || daysOfWeek.Contains("Mon");
        EnableDayTue = daysOfWeek.Contains("Tuesday") || daysOfWeek.Contains("Tue");
        EnableDayWed = daysOfWeek.Contains("Wednesday") || daysOfWeek.Contains("Wed");
        EnableDayThu = daysOfWeek.Contains("Thursday") || daysOfWeek.Contains("Thu");
        EnableDayFri = daysOfWeek.Contains("Friday") || daysOfWeek.Contains("Fri");
        EnableDaySat = daysOfWeek.Contains("Saturday") || daysOfWeek.Contains("Sat");
        EnableDaySun = daysOfWeek.Contains("Sunday") || daysOfWeek.Contains("Sun");
        CloseAlert();
    }

    private async Task BtnClickSave()
    {
        Saving = true;
        ShowAlert("info", "Saving preferences...");
        var req = new SaveMyPrefMarketAlertReq()
        {
            EnableMarketAlertsViaTelegrams = EnableMarketAlertsViaTelegrams,
            MarketAlertTimezone = TimeZone,
            MarketAlertDelayMinutes = MarketAlertDelayMinutes,
            MarketAlertStartTime = StartTime,
            MarketAlertEndTime = EndTime,
            MarketAlertDaysOfWeek = [.. new List<string>()
            {
                EnableDayMon ? "Mon" : null!,
                EnableDayTue ? "Tue" : null!,
                EnableDayWed ? "Wed" : null!,
                EnableDayThu ? "Thu" : null!,
                EnableDayFri ? "Fri" : null!,
                EnableDaySat ? "Sat" : null!,
                EnableDaySun ? "Sun" : null!
            }.Where(day => day != null)],
            TelegramBotApiKey = TelegramBotApiKey.Trim(),
            TelegramChatIDs = TelegramChatIDs.Trim()
        };
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var result = await apiClient.SaveMyPreferencesMarketAlertAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
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
