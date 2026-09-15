using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Shared;

public static class PortfolioUtils
{
    private static readonly Comparer<PortfolioResp> PortfolioComparer = Comparer<PortfolioResp>.Create((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            var cmpName = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            if (cmpName != 0) return cmpName;
            var cmpId = string.Compare(a.Id, b.Id, StringComparison.Ordinal);
            return cmpId;
        });

    public static IEnumerable<PortfolioResp> BuildPortfolioTree(IEnumerable<PortfolioResp> PortfolioList)
    {
        var portfolioSorted = PortfolioList.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        var portfolioDict = portfolioSorted.ToDictionary(p => p.Id);
        var rootPortfolios = new List<PortfolioResp>();

        foreach (var p in portfolioSorted)
        {
            if (!string.IsNullOrEmpty(p.ParentId) && portfolioDict.TryGetValue(p.ParentId, out var parentPortfolio))
            {
                parentPortfolio.Children ??= new SortedSet<PortfolioResp>(PortfolioComparer);
                parentPortfolio.Children.Add(p);
            }
            else
            {
                rootPortfolios.Add(p);
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

    private static IJSObjectReference? jsCharts;
    public static async ValueTask<IJSObjectReference> LoadJSCharts(IJSRuntime JS)
    {
        jsCharts ??= await JS.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(PortfolioUtils).Assembly.GetName().Name!}/js/charts.js"
        );
        return jsCharts;
    }

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
