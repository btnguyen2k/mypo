using System.Text.Json.Serialization;

namespace MyPo.Portfolio.Shared.Identity;

/// <summary>
/// Well-known identifiers for the preference groups stored in a user's metadata.
/// </summary>
public static class PreferenceGroupIds
{
    public const string MarketAlert = "market_alert";
    public const string PortfolioPlan = "portfolio_plan";
}

/// <summary>
/// Public (non-secret) settings for the "Market Alert" preference group.
/// Telegram credentials for this group live in the group's own private data.
/// </summary>
public sealed class MarketAlertPreferences
{
    [JsonPropertyName("via_telegram")]
    public bool ViaTelegram { get; set; } = false;

    [JsonPropertyName("tz")]
    public string Timezone { get; set; } = "UTC";

    [JsonPropertyName("delay_mins")]
    public int DelayMinutes { get; set; } = 60;

    [JsonPropertyName("start")]
    public TimeOnly? StartTime { get; set; } = new TimeOnly(8, 0);

    [JsonPropertyName("end")]
    public TimeOnly? EndTime { get; set; } = new TimeOnly(20, 0);

    [JsonPropertyName("dow")]
    public List<string> DaysOfWeek { get; set; } = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Sunday"];
}

/// <summary>
/// Public (non-secret) settings for the "Portfolio Plan" preference group.
/// Telegram credentials for this group live in the group's own private data.
/// </summary>
public sealed class PortfolioPlanPreferences
{
    [JsonPropertyName("via_telegram")]
    public bool ViaTelegram { get; set; } = false;

    /// <summary>Auto-update portfolio plans every N days. A value &lt;= 0 disables auto-update.</summary>
    [JsonPropertyName("auto_update_days")]
    public int AutoUpdateDays { get; set; } = 0;

    /// <summary>Auto-analyze portfolio plans every N days. A value &lt;= 0 disables auto-analyze.</summary>
    [JsonPropertyName("auto_analyze_days")]
    public int AutoAnalyzeDays { get; set; } = 0;
}
