using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using System.Text.RegularExpressions;

namespace MyPo.Blazor.Portfolio.App.Shared;

public static class PortfolioUtils
{
    public static IEnumerable<PortfolioResp> BuildPortfolioTree(IEnumerable<PortfolioResp> PortfolioList)
    {
        var portfolioSorted = PortfolioList.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        var portfolioDict = portfolioSorted.ToDictionary(p => p.Id);
        var rootPortfolios = new List<PortfolioResp>();

        foreach (var portfolio in portfolioSorted)
        {
            if (!string.IsNullOrEmpty(portfolio.ParentId) && portfolioDict.TryGetValue(portfolio.ParentId, out var parentPortfolio))
            {
                parentPortfolio.Children ??= new SortedSet<PortfolioResp>(Comparer<PortfolioResp>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));
                parentPortfolio.Children.Add(portfolio);
            }
            else
            {
                rootPortfolios.Add(portfolio);
            }
        }

        return rootPortfolios;
    }

    private static IJSObjectReference? jsLocalStorage;

    public static async ValueTask<IJSObjectReference> LoadJSLocalStorage(IJSRuntime JS)
    {
        jsLocalStorage ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(PortfolioUtils).Assembly.GetName().Name!}/js/local-storage.js"
        );
        return jsLocalStorage;
    }

    private static IJSObjectReference? jsDatatable;
    public static async ValueTask<IJSObjectReference> LoadJSDatatable(IJSRuntime JS)
    {
        jsDatatable ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(PortfolioUtils).Assembly.GetName().Name!}/js/datatable.js"
        );
        return jsDatatable;
    }

    private static IJSObjectReference? jsDatetimePicker;
    public static async ValueTask<IJSObjectReference> LoadJSDatetimePicker(IJSRuntime JS)
    {
        jsDatetimePicker ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(PortfolioUtils).Assembly.GetName().Name!}/js/datetime-picker.js"
        );
        return jsDatetimePicker;
    }

    private static IJSObjectReference? jsCoreUIChipInput;
    public static async ValueTask<IJSObjectReference> LoadJSCoreUIChipInput(IJSRuntime JS)
    {
        jsCoreUIChipInput ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(PortfolioUtils).Assembly.GetName().Name!}/js/coreui-chip-input.js"
        );
        return jsCoreUIChipInput;
    }

    // public static string Excerpt(string? input, int maxLength = 60)
    // {
    // 	if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
    // 	{
    // 		return input ?? string.Empty;
    // 	}
    // 	return input.Substring(0, maxLength - 3) + "...";
    // }

    // public const string DEFAULT_DATETIME_PICKER_FORMAT = "dd-MMM-yyyy HH:mm";
    // public static readonly List<string> DATETIME_PICKER_FORMATS =
    // [
    // 	DEFAULT_DATETIME_PICKER_FORMAT,
    // 	"dd-MM-yyyy HH:mm",
    // 	"dd-MMMM-yyyy HH:mm",
    // 	"dd/MM/yyyy HH:mm",
    // 	"dd/MMM/yyyy HH:mm",
    // 	"yyyy-MM-dd HH:mm",
    // 	"yyyy-MM-dd, HH:mm",
    // ];

    // [GeneratedRegex( @"(?<=^|-)(\d)(?=-)", RegexOptions.Compiled)]
    // private static partial Regex MyRegexPaddingDayAndMonth();

    // /// <summary>
    // /// Parse DateTime from datetime picker string, trying multiple formats.
    // /// </summary>
    // /// <param name="dateTimeStr"></param>
    // /// <returns>return null if parsing error</returns>
    // public static DateTime? ParseDateTimeFromDateTimePicker(string dateTimeStr)
    // {
    // 	dateTimeStr = dateTimeStr.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase); // handle Sept to Sep
    // 	dateTimeStr = MyRegexPaddingDayAndMonth().Replace(dateTimeStr, "0$1");

    // 	foreach (var format in DATETIME_PICKER_FORMATS)
    // 	{
    // 		if (DateTime.TryParseExact(dateTimeStr, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
    // 		{
    // 			return dt;
    // 		}
    // 	}
    // 	return null;
    // }

    // /// <summary>
    // /// Parse DateTimeOffset from datetime picker string, trying multiple formats.
    // /// </summary>
    // /// <param name="dateTimeStr"></param>
    // /// <returns>return null if parsing error</returns>
    // public static DateTimeOffset? ParseDateTimeOffsetFromDateTimePicker(string dateTimeStr)
    // {
    // 	var dt = ParseDateTimeFromDateTimePicker(dateTimeStr);
    // 	return dt != null ? new DateTimeOffset(dt.Value) : null;
    // }

    public static decimal EstimateTxFee(MarketDef? market)
    {
        return EstimateTxFee(0, market);
    }

    public static decimal EstimateTxFee(MarketDefResp? market)
    {
        return EstimateTxFee(0, market);
    }

    public static decimal EstimateTxFee(decimal txValue, MarketDefResp? market)
    {
        return EstimateTxFee(txValue, market?.ToModel());
    }

    public static decimal EstimateTxFee(decimal txValue, MarketDef? market)
    {
        return (market?.Country) switch
        {
            "VN" => txValue * 0.15m / 100, // 0.15% for Vietnam market
            "AU" => txValue <= 1000m ? 5.0m : (txValue <= 3000m ? 10.0m : (txValue <= 10000m ? 19.95m : (txValue <= 25000m ? 29.95m : txValue * 0.12m / 100))), // https://www.commsec.com.au/support/rates-and-fees.html
            "US" => Math.Max(5.0m, txValue * 0.12m / 100), // https://www.commsec.com.au/support/rates-and-fees.html
            _ => 0,
        };
    }

    // public static string FormatValueWithScale(double value, decimal scale = 1, string? format = null)
    // {
    // 	return FormatValueWithScale((decimal)value, scale, format);
    // }

    // public static string FormatValueWithScale(decimal value, decimal scale = 1, string? format = null)
    // {
    // 	if (scale <= 0) scale = 1;
    // 	return FormatRawValueWithScale(value*scale, scale, format);
    // }

    // public static string FormatRawValueWithScale(double value, decimal scale = 1, string? format = null)
    // {
    // 	return FormatRawValueWithScale((decimal)value, scale, format);
    // }

    // public static string FormatRawValueWithScale(decimal value, decimal scale = 1, string? format = null)
    // {
    // 	if (scale <= 0) scale = 1;
    // 	if (scale >= 1000)
    // 	{
    // 		return value.ToString(!string.IsNullOrEmpty(format)?format:"N1", System.Globalization.CultureInfo.CurrentCulture);
    // 	}
    // 	if (scale >= 100)
    // 	{
    // 		return value.ToString(!string.IsNullOrEmpty(format)?format:"N2", System.Globalization.CultureInfo.CurrentCulture);
    // 	}
    // 	if (scale > 1)
    // 	{
    // 		return value.ToString(!string.IsNullOrEmpty(format)?format:"N3", System.Globalization.CultureInfo.CurrentCulture);
    // 	}
    // 	return value.ToString(!string.IsNullOrEmpty(format)?format:"N4", System.Globalization.CultureInfo.CurrentCulture);
    // }

    // public static string FormatVolume(int volume)
    // {
    // 	return FormatVolume((long)volume);
    // }

    // public static string FormatVolume(long volume)
    // {
    // 	if (volume >= 1_000_000_000_000)
    // 	{
    // 		return $"{(volume / 1_000_000_000_000m):N2}T";
    // 	}
    // 	if (volume >= 1_000_000_000)
    // 	{
    // 		return $"{(volume / 1_000_000_000m):N2}B";
    // 	}
    // 	if (volume >= 1_000_000)
    // 	{
    // 		return $"{(volume / 1_000_000m):N2}M";
    // 	}
    // 	if (volume >= 1_000)
    // 	{
    // 		return $"{(volume / 1_000m):N2}K";
    // 	}
    // 	return volume.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
    // }

    // public static decimal CalculatePercentageChange(decimal oldValue, decimal newValue)
    // {
    // 	if (oldValue == 0)
    // 	{
    // 		return newValue == 0 ? 0 : 100;
    // 	}
    // 	return ((newValue - oldValue) / Math.Abs(oldValue)) * 100;
    // }

    // public static decimal CalculatePnL(AssetResp asset, MarketDefResp? market, StockQuote quote)
    // {
    // 	return CalculatePnL(asset.ToModel(), market?.ToModel(), quote);
    // }

    // public static decimal CalculatePnL(AssetEntity asset, MarketDef? market, StockQuote quote)
    // {
    // 	if (market == null)
    // 	{
    // 		return 0;
    // 	}
    // 	var currentPrice = quote.MarketPrice;
    // 	var pnl = (currentPrice - asset.AveragePrice*market!.PriceScale) * asset.Quantity;
    // 	return pnl ?? 0;
    // }

    public static string BootstrapCssClassForAnalystRecommendation(string ar)
    {
        return ar switch
        {
            "strong_buy" => "text-success fw-semibold",
            "buy" => "text-success",
            "hold" => "text-warning",
            "sell" => "text-danger",
            "strong_sell" => "text-danger fw-semibold",
            _ => "text-muted",
        };
    }

    public static string BootstrapCssClassForTargetValue(decimal target, decimal current, decimal low, decimal high)
    {
        if (target < low)
        {
            return "text-danger";
        }
        if (target > high)
        {
            return "text-success";
        }
        if (target < current)
        {
            return "text-warning";
        }
        if (target > current)
        {
            return "text-info";
        }
        return "text-muted";
    }

    public static decimal Delta(decimal? oldValue, decimal? newValue)
    {
        return (newValue ?? 0) - (oldValue ?? 0);
    }

    public static string BootstrapCssClassForDelta(decimal? oldValue, decimal? newValue)
    {
        var delta = Delta(oldValue, newValue);
        return delta > 0 ? "text-success" : (delta < 0 ? "text-danger" : "text-muted");
    }
}
