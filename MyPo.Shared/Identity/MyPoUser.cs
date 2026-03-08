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

public sealed class MyPoUserMetadata
{
	[JsonPropertyName("market_alert_timezone")]
	public string? MarketAlertTimezone { get; set; } = "UTC";

	[JsonPropertyName("market_alert_start_time")]
	public TimeOnly? MarketAlertStartTime { get; set; } = new TimeOnly(8, 0); // Default to 8:00 AM

	[JsonPropertyName("market_alert_end_time")]
	public TimeOnly? MarketAlertEndTime { get; set; } = new TimeOnly(18, 0); // Default to 6:00 PM

	[JsonPropertyName("market_alert_days_of_week")]
	public List<string>? MarketAlertDaysOfWeek { get; set; } = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Sunday"];

	[JsonPropertyName("market_alert_via_telegram")]
	public bool MarketAlertViaTelegram { get; set; } = false;

	[JsonIgnore]
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
}
