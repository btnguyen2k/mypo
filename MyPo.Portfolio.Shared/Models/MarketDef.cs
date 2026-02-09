using Microsoft.Extensions.Configuration;

namespace MyPo.Portfolio.Shared.Models;

public sealed class MarketDef
{
	public string Id { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public string Currency { get; set; } = string.Empty;

	public String CurrencySymbol { get; set; } = string.Empty;

	public decimal PriceScale { get; set; } = 1;

	public string ValueFormat { get; set; } = string.Empty;
	public string QuantityFormat { get; set; } = string.Empty;

	public string TimeZone { get; set; } = string.Empty;
	public TimeZoneInfo TZ => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

	public TimeOnly OpenHour { get; set; } = default;

	public TimeOnly CloseHour { get; set; } = default;

	public List<DayOfWeek> TradingDays { get; set; } = [];

	public TimeSpan TimeTillOpen()
	{
		if (IsCurrentlyOpen())
		{
			return TimeSpan.Zero;
		}
		var delta = TimeSpan.Zero;
		var now = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TZ);
		var openDateTime = new DateTimeOffset(now.Year, now.Month, now.Day, OpenHour.Hour, OpenHour.Minute, 0, TZ.GetUtcOffset(now));
		while (true)
		{
			if (delta > TimeSpan.FromHours(72)) return delta; // sanity check to avoid infinite loop in case of misconfiguration
			if (TradingDays.Contains(now.DayOfWeek) && now < openDateTime)
			{
				// today is trading date, not yet open
				return delta + (openDateTime - now);
			}

			// not trading date, or already closed, find next trading date
			var nextDate = now.AddDays(1);
			nextDate = new DateTimeOffset(nextDate.Year, nextDate.Month, nextDate.Day, 0, 0, 0 , TZ.GetUtcOffset(nextDate));
			delta += nextDate - now;
			now = nextDate;
			openDateTime = new DateTimeOffset(now.Year, now.Month, now.Day, OpenHour.Hour, OpenHour.Minute, 0, TZ.GetUtcOffset(now));
		}
	}

	public bool IsCurrentlyOpen()
	{
		var now = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TZ);
		if (!TradingDays.Contains(now.DayOfWeek))
		{
			return false;
		}
		var currentTime = TimeOnly.FromDateTime(now.DateTime);
		return currentTime >= OpenHour && currentTime <= CloseHour;
	}

	public static MarketDef Build(string id, IConfigurationSection data)
	{
		var marketDef = data.Get<MarketDef>()!;
		marketDef.Id = id;
		marketDef.OpenHour = TimeOnly.Parse(data["trading_hours:open"] ?? "00:00");
		marketDef.CloseHour = TimeOnly.Parse(data["trading_hours:close"] ?? "23:59");
		var tradingDays = data.GetSection("trading_days").Get<List<string>>() ?? [];
		marketDef.TradingDays = [.. tradingDays.Select(dayStr => Enum.Parse<DayOfWeek>(dayStr, ignoreCase: true))];
		marketDef.TZ.GetUtcOffset(DateTimeOffset.Now); // validate time zone
		marketDef.CurrencySymbol = data.GetSection("currency_symbol").Value ?? string.Empty;
		marketDef.PriceScale = data.GetValue<decimal?>("price_scale") ?? 1;
		marketDef.ValueFormat = data.GetValue<string?>("value_format") ?? string.Empty;
		marketDef.QuantityFormat = data.GetValue<string?>("quantity_format") ?? string.Empty;
		return marketDef;
	}
}
