using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Utils;

public static class MarketEventUtils
{
	/// <summary>
	/// Determine the attention level of a Dividend/Distribution event based on its amount and yield.
	/// </summary>
	/// <param name="e"></param>
	/// <param name="yieldsMap"></param>
	/// <returns>The attention level: 0 (no attention), 1 (low), 2 (medium), 3 (high)</returns>
	public static int AttentionLevelForDividend(MarketEventEntity e, IDictionary<string, decimal> yieldsMap)
	{
		if (e.MarketId.Equals("VN", StringComparison.OrdinalIgnoreCase))
		{
			if (e.Metadata?.Dividend?.Amount >= 3500)
			{
				return 3;
			}
			if (yieldsMap.TryGetValue(e.ItemCode, out var yieldVN))
			{
				if (Globals.IndexConstituents.TryGetValue("VN30", out var vn30) && (vn30?.Contains(e.ItemCode)??false))
				{
					return yieldVN >= 0.04m ? 2 : yieldVN >= 0.02m ? 1 : 0;
				}
				if (Globals.IndexConstituents.TryGetValue("VN100", out var vn100) && (vn100?.Contains(e.ItemCode)??false))
				{
					return yieldVN >= 0.06m ? 2 : yieldVN >= 0.03m ? 1 : 0;
				}
				if (Globals.IndexConstituents.TryGetValue("HNX30", out var hnx30) && (hnx30?.Contains(e.ItemCode)??false))
				{
					return yieldVN >= 0.05m ? 2 : yieldVN >= 0.025m ? 1 : 0;
				}
			}
		}
		if (e.MarketId.Equals("AU", StringComparison.OrdinalIgnoreCase) && yieldsMap.TryGetValue(e.ItemCode, out var yieldAU))
		{
			var marketCap = e.Metadata?.Capital ?? 0;
			if (marketCap >= 10_000_000_000) // large cap
			{
				return yieldAU >= 0.045m ? 3 : yieldAU >= 0.03m ? 2 : yieldAU >= 0.02m ? 1 : 0;
			}
			else if (marketCap >= 2_000_000_000) // mid cap
			{
				return yieldAU >= 0.065m ? 3 : yieldAU >= 0.045m ? 2 : yieldAU >= 0.03m ? 1 : 0;
			}
			else if (marketCap >= 300_000_000) // small cap
			{
				return yieldAU >= 0.085m ? 3 : yieldAU >= 0.065m ? 2 : yieldAU >= 0.045m ? 1 : 0;
			}
		}
		if (e.MarketId.Equals("US", StringComparison.OrdinalIgnoreCase) && yieldsMap.TryGetValue(e.ItemCode, out var yieldUS))
		{
			var marketCap = e.Metadata?.Capital ?? 0;
			if (marketCap >= 10_000_000_000) // large cap
			{
				if (e.ItemCode.StartsWith("NASDAQ:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.015m ? 3 : yieldUS >= 0.01m ? 2 : yieldUS >= 0.005m ? 1 : 0;
				}
				if (e.ItemCode.StartsWith("NYSE:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.025m ? 3 : yieldUS >= 0.015m ? 2 : yieldUS >= 0.01m ? 1 : 0;
				}
			}
			else if (marketCap >= 2_000_000_000) // mid cap
			{
				if (e.ItemCode.StartsWith("NASDAQ:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.01m ? 2 : yieldUS >= 0.005m ? 1 : 0;
				}
				if (e.ItemCode.StartsWith("NYSE:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.015m ? 2 : yieldUS >= 0.01m ? 1 : 0;
				}
			}
			else if (marketCap >= 300_000_000) // small cap
			{
				if (e.ItemCode.StartsWith("NASDAQ:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.005m ? 1 : 0;
				}
				if (e.ItemCode.StartsWith("NYSE:", StringComparison.OrdinalIgnoreCase))
				{
					return yieldUS >= 0.01m ? 1 : 0;
				}
			}
		}

		return 0;
	}

	/// <summary>
	/// Map market ID to its default time zone ID.
	/// </summary>
	/// <param name="marketId"></param>
	/// <returns></returns>
	public static string MarketToDefaultTimeZoneId(string marketId)
	{
		return marketId.ToUpper() switch
		{
			"AU" => "Australia/Sydney",
			"US" => "America/New_York",
			"VN" => "Asia/Ho_Chi_Minh",
			_ => "UTC"
		};
	}
}
