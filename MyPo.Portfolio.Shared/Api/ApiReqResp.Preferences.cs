using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Api;

public sealed class SaveMyPrefMarketAlertReq
{
	[JsonPropertyName("enable_market_alerts_via_telegrams")]
	public bool EnableMarketAlertsViaTelegrams { get; set; } = false;

	[JsonPropertyName("market_alert_timezone")]
	public string MarketAlertTimezone { get; set; } = "UTC";

	[JsonPropertyName("market_alert_delay_minutes")]
	public int MarketAlertDelayMinutes { get; set; } = 60;

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

/// <summary>
/// Request to save the current user's "Portfolio Plan" preferences.
/// </summary>
public sealed class SaveMyPrefPortfolioPlanReq
{
	/// <summary>
	/// Whether to send portfolio-plan alerts via Telegram.
	/// </summary>
	[JsonPropertyName("enable_portfolio_plan_alerts_via_telegrams")]
	public bool EnablePortfolioPlanAlertsViaTelegrams { get; set; } = false;

	/// <summary>
	/// Auto-update the user's portfolio plans every N days. A value &lt;= 0 disables auto-update.
	/// </summary>
	[JsonPropertyName("portfolio_plan_auto_update_days")]
	public int PortfolioPlanAutoUpdateDays { get; set; } = 0;

	/// <summary>
	/// Auto-analyze the user's portfolio plans every N days. A value &lt;= 0 disables auto-analyze.
	/// </summary>
	[JsonPropertyName("portfolio_plan_auto_analyze_days")]
	public int PortfolioPlanAutoAnalyzeDays { get; set; } = 0;

	[JsonPropertyName("telegram_bot_api_key"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TelegramBotApiKey { get; set; }

	[JsonPropertyName("telegram_chat_ids"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TelegramChatIDs { get; set; }
}
