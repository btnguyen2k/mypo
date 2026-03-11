using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Utils;

public static class YFUtils
{
	/// <summary>
	/// Builds Yahoo Finance symbol ticker for the given input, using the market definition to determine the exchange suffix if needed.
	/// </summary>
	/// <param name="symbolCode"></param>
	/// <param name="market"></param>
	/// <returns></returns>
	public static string BuildYFTicker(string symbolCode, MarketDef? market = null)
	{
		return market?.Code switch
		{
			"*AUSYD" or "ASX" => $"{symbolCode}.AX",
			"*VNVN" or "HOSE" or "HNX" or "UPCOM" => $"{symbolCode}.VN",
			_ => symbolCode,
		};
	}

	/// <summary>
	/// Builds Yahoo Finance symbol ticker for the given input, accepting format EXCHANGE:TICKER. If the exchange is recognized, it will be used to determine the exchange suffix for Yahoo Finance.
	/// </summary>
	/// <param name="exchangeCode"></param>
	/// <returns></returns>
	public static string BuildYFTicker(string exchangeSymbolCode)
	{
		var (exchange, code) = exchangeSymbolCode.Split(':', 2) switch
		{
			var arr when arr.Length == 2 => (arr[0], arr[1]),
			_ => (string.Empty, exchangeSymbolCode),
		};
		return exchange.ToUpper() switch
		{
			"ASX" => $"{code}.AX",
			"HOSE" or "HNX" or "UPCOM" => $"{code}.VN",
			_ => code,
		};
	}
}
