using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public sealed class MarketDefResp
{
	public const string TIMEONLY_FORMAT = "HH:mm";
	public static MarketDefResp BuildFrom(MarketDef md)
	{
		return new MarketDefResp
		{
			Id = md.Id,
			Code = md.Code,
			Name = md.Name,
			Country = md.Country,
			Currency = md.Currency,
			CurrencySymbol = md.CurrencySymbol,
			PriceScale = md.PriceScale,
			ValueFormat = md.ValueFormat,
			QuantityFormat = md.QuantityFormat,
			TimeZone = md.TimeZone,
			OpenHour = md.OpenHour.ToString(TIMEONLY_FORMAT),
			CloseHour = md.CloseHour.ToString(TIMEONLY_FORMAT),
			TradingDays = [.. md.TradingDays.Select(d => d.ToString())]
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("code")]
	public string Code { get; set; } = default!;

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("country")]
	public string Country { get; set; } = default!;

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = default!;

	[JsonPropertyName("currency_symbol")]
	public string CurrencySymbol { get; set; } = default!;

	[JsonPropertyName("price_scale")]
	public decimal PriceScale { get; set; }

	[JsonPropertyName("value_format")]
	public string ValueFormat { get; set; } = default!;

	[JsonPropertyName("quantity_format")]
	public string QuantityFormat { get; set; } = default!;

	[JsonPropertyName("timezone")]
	public string TimeZone { get; set; } = default!;

	[JsonPropertyName("open_hour")]
	public string OpenHour { get; set; } = default!;

	[JsonPropertyName("close_hour")]
	public string CloseHour { get; set; } = default!;

	[JsonPropertyName("trading_days")]
	public List<String> TradingDays { get; set; } = default!;

	public MarketDef ToModel() => new()
	{
		Id = this.Id,
		Code = this.Code,
		Name = this.Name,
		Country = this.Country,
		Currency = this.Currency,
		CurrencySymbol = this.CurrencySymbol,
		PriceScale = this.PriceScale,
		ValueFormat = this.ValueFormat,
		QuantityFormat = this.QuantityFormat,
		TimeZone = this.TimeZone,
		OpenHour = TimeOnly.ParseExact(this.OpenHour, TIMEONLY_FORMAT),
		CloseHour = TimeOnly.ParseExact(this.CloseHour, TIMEONLY_FORMAT),
		TradingDays = [.. this.TradingDays.Select(d => Enum.Parse<DayOfWeek>(d))]
	};
}
