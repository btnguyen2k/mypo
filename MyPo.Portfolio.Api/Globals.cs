using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api;

public sealed class Globals
{
	private sealed class MarketDefinitionComparer : IComparer<MarketDefinition>
	{
		public static readonly MarketDefinitionComparer Instance = new();
		public int Compare(MarketDefinition? x, MarketDefinition? y)
		{
			if (x == null && y == null) return 0;
			if (x == null) return -1;
			if (y == null) return 1;
			var cmpCountry = string.Compare(x.Country, y.Country, StringComparison.OrdinalIgnoreCase);
			return cmpCountry != 0 ? cmpCountry : string.Compare(x.Code, y.Code, StringComparison.OrdinalIgnoreCase);
		}
	}

	public static readonly ISet<MarketDefinition> Markets = new SortedSet<MarketDefinition>(MarketDefinitionComparer.Instance);
}
