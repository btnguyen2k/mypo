using Microsoft.Extensions.Configuration;

namespace MyPo.Portfolio.Shared.Models;

public sealed class MarketDef
{
	public string Id { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public string Currency { get; set; } = string.Empty;

	public string TimeZone { get; set; } = string.Empty;

	public TimeOnly OpenHour { get; set; } = default;

	public TimeOnly CloseHour { get; set; } = default;

	public List<DayOfWeek> TradingDays { get; set; } = [];

	public static MarketDef Build(string id, IConfigurationSection data)
	{
		var marketDef = data.Get<MarketDef>()!;
		marketDef.Id = id;
		marketDef.OpenHour = TimeOnly.Parse(data["trading_hours:open"] ?? "00:00");
		marketDef.CloseHour = TimeOnly.Parse(data["trading_hours:close"] ?? "23:59");
		var tradingDays = data.GetSection("trading_days").Get<List<string>>() ?? [];
		marketDef.TradingDays = [.. tradingDays.Select(dayStr => Enum.Parse<DayOfWeek>(dayStr, ignoreCase: true))];
		return marketDef;
	}
}
