using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPreferences : BasePage
{
	[Inject]
	private ILogger<MyPortfolio>? Logger { get; set; }

	private bool EnableMarketAlertsViaTelegrams { get; set; } = false;
	private string TimeZone { get; set; } = "";
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
	private string Temp { get; set; } = "-1001234567890 &#10; 987654321 &#10; -4412345678";

	/// <inheritdoc />
	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender && CurrentUser != null)
		{
			Console.WriteLine(JsonSerializer.Serialize(CurrentUser));
			// EnableMarketAlertsViaTelegrams = CurrentUser.Metadata?.MarketAlertViaTelegram ?? false;
			// TimeZone = CurrentUser.Metadata?.MarketAlertTimezone ?? "UTC";
			// StartTime = CurrentUser.Metadata?.MarketAlertStartTime ?? new TimeOnly(8, 0);
			// EndTime = CurrentUser.Metadata?.MarketAlertEndTime ?? new TimeOnly(20, 0);
			// StateHasChanged();
		}
	}

	private void BtnClickSave()
	{
		Console.WriteLine("Saving preferences...");
		Console.WriteLine("Enable: {0}", EnableMarketAlertsViaTelegrams);
		Console.WriteLine("TimeZone: {0}", TimeZone);
		Console.WriteLine("Start: {0}", StartTime);
		Console.WriteLine("End: {0}", EndTime);
		Console.WriteLine("Mon: {0}", EnableDayMon);
		Console.WriteLine("Tue: {0}", EnableDayTue);
		Console.WriteLine("Wed: {0}", EnableDayWed);
		Console.WriteLine("Thu: {0}", EnableDayThu);
		Console.WriteLine("Fri: {0}", EnableDayFri);
		Console.WriteLine("Sat: {0}", EnableDaySat);
		Console.WriteLine("Sun: {0}", EnableDaySun);
		Console.WriteLine("TelegramBotApiKey: {0}", TelegramBotApiKey);
		Console.WriteLine("TelegramChatIDs: {0}", TelegramChatIDs);
	}
}
