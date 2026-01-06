using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace MyPo.Portfolio.Shared.Models;

public sealed class MarketDefinition
{
	[JsonPropertyName("code")]
	public string Code { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("country")]
	public string Country { get; set; } = string.Empty;

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("timezone")]
	public string TimeZone { get; set; } = string.Empty;

	[JsonPropertyName("open_hour")]
	public TimeOnly OpenHour { get; set; } = default;

	[JsonPropertyName("close_hour")]
	public TimeOnly CloseHour { get; set; } = default;

	[JsonPropertyName("trading_days")]
	public List<DayOfWeek> TradingDays { get; set; } = [];

	public static MarketDefinition Build(string code, IConfigurationSection data)
	{
		var marketDef = data.Get<MarketDefinition>()!;
		marketDef.Code = code;
		marketDef.OpenHour = TimeOnly.Parse(data["trading_hours:open"] ?? "00:00");
		marketDef.CloseHour = TimeOnly.Parse(data["trading_hours:close"] ?? "23:59");
		var tradingDays = data.GetSection("trading_days").Get<List<string>>() ?? [];
		marketDef.TradingDays = [.. tradingDays.Select(dayStr => Enum.Parse<DayOfWeek>(dayStr, ignoreCase: true))];
		return marketDef;
	}
}
