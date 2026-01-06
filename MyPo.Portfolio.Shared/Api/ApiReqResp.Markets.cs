using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct MarketDefResp
{
	public static MarketDefResp BuildFrom(MarketDef md)
	{
		return new MarketDefResp
		{
			Id = md.Id,
			Code = md.Code,
			Name = md.Name,
			Country = md.Country,
			Currency = md.Currency,
			TimeZone = md.TimeZone,
			OpenHour = md.OpenHour.ToString("HH:mm"),
			CloseHour = md.CloseHour.ToString("HH:mm"),
			TradingDays = [.. md.TradingDays.Select(d => d.ToString())]
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("code")]
	public string Code { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("country")]
	public string Country { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; }

	[JsonPropertyName("timezone")]
	public string TimeZone { get; set; }

	[JsonPropertyName("open_hour")]
	public string OpenHour { get; set; }

	[JsonPropertyName("close_hour")]
	public string CloseHour { get; set; }

	[JsonPropertyName("trading_days")]
	public List<String> TradingDays { get; set; }
}
