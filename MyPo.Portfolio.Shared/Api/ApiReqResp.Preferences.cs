using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Api;

public sealed class SaveMyPrefMarketAlertReq
{
	[JsonPropertyName("enable_market_alerts_via_telegrams")]
	public bool EnableMarketAlertsViaTelegrams { get; set; } = false;

	[JsonPropertyName("market_alert_timezone")]
	public string MarketAlertTimezone { get; set; } = "UTC";

	[JsonPropertyName("market_alert_start_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public TimeOnly? MarketAlertStartTime { get; set; }

	[JsonPropertyName("market_alert_end_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public TimeOnly? MarketAlertEndTime { get; set; }

	[JsonPropertyName("market_alert_days_of_week"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? MarketAlertDaysOfWeek { get; set; }

	[JsonPropertyName("telegram_bot_api_key"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TelegramBotApiKey { get; set; }

	[JsonPropertyName("telegram_chat_ids"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TelegramChatIDs { get; set; }
}
