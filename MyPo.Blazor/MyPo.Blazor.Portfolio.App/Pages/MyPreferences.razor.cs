using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPreferences : BasePage
{
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
		if (CurrentUser != null)
		{
			EnableMarketAlertsViaTelegrams = CurrentUser.Value.Metadata?.MarketAlertViaTelegram ?? false;
			TimeZone = CurrentUser.Value.Metadata?.MarketAlertTimezone ?? "UTC";
			MarketAlertDelayMinutes = CurrentUser.Value.Metadata?.MarketAlertDelayMinutes ?? 60;
			StartTime = CurrentUser.Value.Metadata?.MarketAlertStartTime ?? new TimeOnly(8, 0);
			EndTime = CurrentUser.Value.Metadata?.MarketAlertEndTime ?? new TimeOnly(20, 0);
			EnableDayMon = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Monday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Mon") ?? false);
			EnableDayTue = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Tuesday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Tue") ?? false);
			EnableDayWed = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Wednesday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Wed") ?? false);
			EnableDayThu = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Thursday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Thu") ?? false);
			EnableDayFri = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Friday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Fri") ?? false);
			EnableDaySat = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Saturday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Sat") ?? false);
			EnableDaySun = (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Sunday") ?? false)
				|| (CurrentUser.Value.Metadata?.MarketAlertDaysOfWeek?.Contains("Sun") ?? false);
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			var (alertType, alertMessage) = GetPassedMessageFromQuery();
			if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
			{
				ShowAlert(alertType, alertMessage, ALERT_AUTO_CLOSE_MS);
			}
			else
			{
				CloseAlert();
			}
		}
	}

	private async Task BtnClickSave()
	{
		HideUI = true;
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
		HideUI = false;
	}
}
