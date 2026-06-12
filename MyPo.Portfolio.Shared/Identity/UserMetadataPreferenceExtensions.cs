using System.Text.Json;
using System.Text.RegularExpressions;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.Identity;

/// <summary>
/// Domain-level helpers that bind strongly-typed preference groups (and their isolated Telegram
/// credentials) onto the generic <see cref="MyPoUserMetadata"/> preference-group store.
/// </summary>
public static partial class UserMetadataPreferenceExtensions
{
    private const string SECRET_TELEGRAM_BOT_API_KEY = "telegram_bot_api_key";
    private const string SECRET_TELEGRAM_CHAT_IDS = "telegram_chat_ids";

    // ------------------------------------------------------------------ Market Alert

    public static MarketAlertPreferences GetMarketAlertPreferences(this MyPoUserMetadata metadata)
        => metadata.GetPreferenceGroup(PreferenceGroupIds.MarketAlert)?.GetSettings<MarketAlertPreferences>()
            ?? new MarketAlertPreferences();

    public static void SetMarketAlertPreferences(this MyPoUserMetadata metadata, MarketAlertPreferences prefs)
        => metadata.GetOrCreatePreferenceGroup(PreferenceGroupIds.MarketAlert).SetSettings(prefs);

    // ------------------------------------------------------------------ Portfolio Plan

    public static PortfolioPlanPreferences GetPortfolioPlanPreferences(this MyPoUserMetadata metadata)
        => metadata.GetPreferenceGroup(PreferenceGroupIds.PortfolioPlan)?.GetSettings<PortfolioPlanPreferences>()
            ?? new PortfolioPlanPreferences();

    public static void SetPortfolioPlanPreferences(this MyPoUserMetadata metadata, PortfolioPlanPreferences prefs)
        => metadata.GetOrCreatePreferenceGroup(PreferenceGroupIds.PortfolioPlan).SetSettings(prefs);

    // ------------------------------------------------------------------ Per-group Telegram secrets

    public static string? GetMarketAlertTelegramBotApiKey(this MyPoUserMetadata metadata)
        => metadata.GetGroupTelegramBotApiKey(PreferenceGroupIds.MarketAlert);

    public static IEnumerable<string> GetMarketAlertTelegramChatIDs(this MyPoUserMetadata metadata)
        => metadata.GetGroupTelegramChatIDs(PreferenceGroupIds.MarketAlert);

    public static string? GetPortfolioPlanTelegramBotApiKey(this MyPoUserMetadata metadata)
        => metadata.GetGroupTelegramBotApiKey(PreferenceGroupIds.PortfolioPlan);

    public static IEnumerable<string> GetPortfolioPlanTelegramChatIDs(this MyPoUserMetadata metadata)
        => metadata.GetGroupTelegramChatIDs(PreferenceGroupIds.PortfolioPlan);

    /// <summary>Stores the Telegram bot API key in the given group's isolated secret bag.</summary>
    public static void SetTelegramBotApiKey(this PreferenceGroupData group, string apiKey)
        => group.SetSecret(SECRET_TELEGRAM_BOT_API_KEY, apiKey);

    /// <summary>Stores Telegram chat IDs (raw, free-form text) in the given group's secret bag.</summary>
    public static void SetTelegramChatIDs(this PreferenceGroupData group, string chatIDs)
    {
        var ids = RegexSplitTelegramChatIDs().Split(chatIDs)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet();
        group.SetSecret(SECRET_TELEGRAM_CHAT_IDS, JsonSerializer.Serialize(ids));
    }

    // ------------------------------------------------------------------ Internals

    private static string? GetGroupTelegramBotApiKey(this MyPoUserMetadata metadata, string groupId)
        => metadata.GetPreferenceGroup(groupId)?.GetSecret(SECRET_TELEGRAM_BOT_API_KEY);

    private static IEnumerable<string> GetGroupTelegramChatIDs(this MyPoUserMetadata metadata, string groupId)
    {
        var json = metadata.GetPreferenceGroup(groupId)?.GetSecret(SECRET_TELEGRAM_CHAT_IDS);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }
        return JsonSerializer.Deserialize<IEnumerable<string>>(json) ?? [];
    }

    [GeneratedRegex(@"[\s,;]+")]
    private static partial Regex RegexSplitTelegramChatIDs();
}
