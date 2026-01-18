using Finance.Net.Models.Yahoo;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

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

	public static string FormatValueWithScale(double value, decimal scale = 1, string? format = null)
	{
		return FormatValueWithScale((decimal)value, scale, format);
	}

	public static string FormatValueWithScale(decimal value, decimal scale = 1, string? format = null)
	{
		if (scale <= 0) scale = 1;
		value *= scale;
		return FormatRawValueWithScale(value, scale, format);
		// if (scale >= 1000)
		// {
		// 	return value.ToString(!string.IsNullOrEmpty(format)?format:"N1", System.Globalization.CultureInfo.CurrentCulture);
		// }
		// if (scale >= 100)
		// {
		// 	return value.ToString(!string.IsNullOrEmpty(format)?format:"N2", System.Globalization.CultureInfo.CurrentCulture);
		// }
		// if (scale > 1)
		// {
		// 	return value.ToString(!string.IsNullOrEmpty(format)?format:"N3", System.Globalization.CultureInfo.CurrentCulture);
		// }
		// return value.ToString(!string.IsNullOrEmpty(format)?format:"N4", System.Globalization.CultureInfo.CurrentCulture);
	}

	public static string FormatRawValueWithScale(double value, decimal scale = 1, string? format = null)
	{
		return FormatRawValueWithScale((decimal)value, scale, format);
	}

	public static string FormatRawValueWithScale(decimal value, decimal scale = 1, string? format = null)
	{
		if (scale <= 0) scale = 1;
		if (scale >= 1000)
		{
			return value.ToString(!string.IsNullOrEmpty(format)?format:"N1", System.Globalization.CultureInfo.CurrentCulture);
		}
		if (scale >= 100)
		{
			return value.ToString(!string.IsNullOrEmpty(format)?format:"N2", System.Globalization.CultureInfo.CurrentCulture);
		}
		if (scale > 1)
		{
			return value.ToString(!string.IsNullOrEmpty(format)?format:"N3", System.Globalization.CultureInfo.CurrentCulture);
		}
		return value.ToString(!string.IsNullOrEmpty(format)?format:"N4", System.Globalization.CultureInfo.CurrentCulture);
	}

	public static decimal CalculatePercentageChange(decimal oldValue, decimal newValue)
	{
		if (oldValue == 0)
		{
			return newValue == 0 ? 0 : 100;
		}
		return ((newValue - oldValue) / Math.Abs(oldValue)) * 100;
	}

	public static decimal CalculatePnL(AssetResp asset, MarketDefResp? market, Quote quote)
	{
		return CalculatePnL(asset.ToModel(), market?.ToModel(), quote);
	}

	public static decimal CalculatePnL(Asset asset, MarketDef? market, Quote quote)
	{
		if (market == null)
		{
			return 0;
		}
		var currentPrice = quote.RegularMarketPrice ?? 0;
		var pnl = ((decimal)currentPrice - asset.AveragePrice*market!.PriceScale) * asset.Quantity;
		return pnl;
	}
}
