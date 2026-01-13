using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Shared;

public static class PortfolioUtils
{
    public static IEnumerable<PortfolioRecResp> BuildPortfolioTree(IEnumerable<PortfolioRecResp> PortfolioList)
    {
        var portfolioSorted = PortfolioList.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        var portfolioDict = portfolioSorted.ToDictionary(p => p.Id);
        var rootPortfolios = new List<PortfolioRecResp>();

        foreach (var portfolio in portfolioSorted)
        {
            if (!string.IsNullOrEmpty(portfolio.ParentId) && portfolioDict.TryGetValue(portfolio.ParentId, out var parentPortfolio))
            {
                parentPortfolio.Children ??= new SortedSet<PortfolioRecResp>(Comparer<PortfolioRecResp>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));
                parentPortfolio.Children.Add(portfolio);
            }
            else
            {
                rootPortfolios.Add(portfolio);
            }
        }

        return rootPortfolios;
    }

	public static string FormatValueWithScale(decimal value, decimal scale = 1)
	{
		if (scale <= 0) scale = 1;
		value *= scale;
		if (scale >= 1000)
		{
			return value.ToString($"N0", System.Globalization.CultureInfo.CurrentCulture);
		}
		if (scale >= 100)
		{
			return value.ToString($"N1", System.Globalization.CultureInfo.CurrentCulture);
		}
		if (scale > 1)
		{
			return value.ToString($"N2", System.Globalization.CultureInfo.CurrentCulture);
		}
		return value.ToString($"N4", System.Globalization.CultureInfo.CurrentCulture);
	}
}
