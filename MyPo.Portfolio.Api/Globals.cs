using System.Collections.Concurrent;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Portfolio.Api;

public sealed class Globals
{
	private sealed class MarketDefComparer : IComparer<MarketDef>
	{
		public static readonly MarketDefComparer Instance = new();
		public int Compare(MarketDef? x, MarketDef? y)
		{
			if (x == null && y == null) return 0;
			if (x == null) return -1;
			if (y == null) return 1;
			var cmpCountry = string.Compare(x.Country, y.Country, StringComparison.OrdinalIgnoreCase);
			return cmpCountry != 0 ? cmpCountry : string.Compare(x.Code, y.Code, StringComparison.OrdinalIgnoreCase);
		}
	}

	public static readonly ISet<MarketDef> Markets = new SortedSet<MarketDef>(MarketDefComparer.Instance);
	public static readonly ConcurrentDictionary<string, MarketDef> MarketsMap = [];

	private sealed class AIVendorComparer : IComparer<AIVendor>
	{
		public static readonly AIVendorComparer Instance = new();
		public int Compare(AIVendor? x, AIVendor? y)
		{
			if (x == null && y == null) return 0;
			if (x == null) return -1;
			if (y == null) return 1;
			return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	public static readonly ISet<AIVendor> AIVendors = new SortedSet<AIVendor>(AIVendorComparer.Instance);
	public static readonly ConcurrentDictionary<string, AIVendor> AIVendorsMap = [];
}
