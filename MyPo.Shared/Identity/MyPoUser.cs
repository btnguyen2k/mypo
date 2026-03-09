using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using MyPo.Libs.Clavis;

namespace MyPo.Shared.Identity;

public sealed class MyPoUser : IdentityUser
{
	public IEnumerable<MyPoRole>? Roles { get; set; } = default!;
	public IEnumerable<IdentityUserClaim<string>>? Claims { get; set; } = default!;

	public string? GivenName { get; set; } = default!;
	public string? FamilyName { get; set; } = default!;

	public MyPoUserMetadata? Metadata { get; set; } = default!;

	/// <summary>
	/// Touches the entity, updating the <see cref="ConcurrencyStamp"/> property.
	/// </summary>
	public void Touch() => ConcurrencyStamp = Guid.NewGuid().ToString();

	public override bool Equals(object? obj) => obj is MyPoUser other
		&& (ReferenceEquals(this, other) || Id.Equals(other.Id, Globals.StringComparison));

	public override int GetHashCode() => Id.GetHashCode(Globals.StringComparison);
}

public sealed partial class MyPoUserMetadata
{
	[JsonPropertyName("mk_alert_tz")]
	public string? MarketAlertTimezone { get; set; } = "UTC";

	[JsonPropertyName("mk_alert_start")]
	public TimeOnly? MarketAlertStartTime { get; set; } = new TimeOnly(8, 0); // Default to 8:00 AM

	[JsonPropertyName("mk_alert_end")]
	public TimeOnly? MarketAlertEndTime { get; set; } = new TimeOnly(20, 0); // Default to 6:00 PM

	[JsonPropertyName("mk_alert_dow")]
	public List<string>? MarketAlertDaysOfWeek { get; set; } = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Sunday"];

	[JsonPropertyName("mk_alert_telegram")]
	public bool MarketAlertViaTelegram { get; set; } = false;

	[JsonPropertyName("private_data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PrivateData? PrivateData { get; set; }

	private void InitPrivateData()
	{
		PrivateData ??= new PrivateData();
	}

	public string? GetTelegramBotApiKey() => PrivateData?.Get("telegram_bot_api_key");
	public void SetTelegramBotApiKey(string apiKey)
	{
		InitPrivateData();
		PrivateData!.Add("telegram_bot_api_key", apiKey);
	}

	public IEnumerable<string> GetTelegramChatIDs()
	{
		var chatIDsJson = PrivateData?.Get("telegram_chat_ids");
		var obj = JsonSerializer.Deserialize<IEnumerable<string>>(chatIDsJson ?? "[]");
		return obj ?? [];
	}
	public void SetTelegramChatIDs(IEnumerable<string> chatIDs)
	{
		InitPrivateData();
		PrivateData!.Add("telegram_chat_ids", JsonSerializer.Serialize(chatIDs.ToHashSet()));
	}
	public void SetTelegramChatIDs(string chatIDs)
	{
		// use regex to split by space, comma, semicolon, or newline, and remove empty entries
		var regex = MyRegexSplitTelegramChatIDs();
		var ids = regex.Split(chatIDs).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim());
		SetTelegramChatIDs(ids);
	}

	[System.Text.RegularExpressions.GeneratedRegex(@"[\s,;]+")]
	private static partial System.Text.RegularExpressions.Regex MyRegexSplitTelegramChatIDs();
}
