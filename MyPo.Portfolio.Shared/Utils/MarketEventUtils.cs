using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Utils;

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
			if (e.Metadata?.Dividend?.Amount >= 3000)
			{
				return 3;
			}
			else if (yieldsMap.TryGetValue(e.ItemCode, out var yield) && yield >= 0.04m)
			{
				return 2;
			}
		}
		else if (yieldsMap.TryGetValue(e.ItemCode, out var yield))
		{
			if (e.Metadata?.Dividend?.Amount >= 1.00m && yield >= 0.04m)
			{
				return 3;
			}
			if (e.Metadata?.Dividend?.Amount >= 5.00m && yield >= 0.02m)
			{
				return 2;
			}
			if (e.Metadata?.Dividend?.Amount >= 0.03m && yield >= 0.07m)
			{
				return 2;
			}
			if (e.Metadata?.Dividend?.Amount >= 0.03m && yield >= 0.03m)
			{
				return 1;
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
