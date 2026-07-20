using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTrends : CBase
{
    public sealed class TrendBucket
    {
        public string Label { get; set; } = string.Empty;
        /// <summary>Report period start (yyyy-MM-dd) - used to fetch the latest period's allocation.</summary>
        public string PeriodStart { get; set; } = string.Empty;
        /// <summary>Net asset value at the period start (holdings value + cash balance).</summary>
        public decimal OpeningValue { get; set; }
        /// <summary>Net asset value at the period end (holdings value + cash balance).</summary>
        public decimal Value { get; set; }
        /// <summary>Period equity profit/loss (excludes external cash deposits/withdrawals).</summary>
        public decimal NetPnl { get; set; }
        /// <summary>External net cash flow during the period (deposits - withdrawals).</summary>
        public decimal NetCashFlow { get; set; }
        /// <summary>Period return relative to the opening NAV.</summary>
        public decimal NetPnlPct => OpeningValue != 0 ? NetPnl / OpeningValue : 0;
    }

    public sealed class AllocationSlice
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Weight { get; set; }
    }

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private MarketDefResp? Market = null;

    private ReportType SelectedGrouping { get; set; } = ReportType.MONTHLY;
    private int SelectedCount { get; set; } = 12;

    private bool Generating { get; set; }

    private IReadOnlyList<TrendBucket> Buckets { get; set; } = [];
    private IReadOnlyList<AllocationSlice> Allocation { get; set; } = [];

    private object? ValueChartConfig { get; set; }
    private object? PnlChartConfig { get; set; }
    private object? AllocationChartConfig { get; set; }

    private string Currency => Market?.CurrencySymbol ?? Portfolio?.Currency ?? "USD";

    private string? _loadedPortfolioId;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // Initialise once per portfolio so background re-renders don't clobber the user's selections.
        if (Portfolio is not null && !string.Equals(Portfolio.Id, _loadedPortfolioId, StringComparison.Ordinal))
        {
            _loadedPortfolioId = Portfolio.Id;
            Market = Markets?.FirstOrDefault(m => string.Equals(m.Id, Portfolio.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));
            SelectedGrouping = ReportType.MONTHLY;
            SelectedCount = DefaultCount(SelectedGrouping);
            ResetReport();
        }
    }

    private void SelectGrouping(ReportType type)
    {
        if (SelectedGrouping != type)
        {
            SelectedGrouping = type;
            SelectedCount = DefaultCount(type);
            // The displayed charts are now stale; require an explicit Generate.
            ResetReport();
        }
    }

    private void OnCountChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var count))
        {
            SelectedCount = count;
            ResetReport();
        }
    }

    private void ResetReport()
    {
        Buckets = [];
        Allocation = [];
        ValueChartConfig = null;
        PnlChartConfig = null;
        AllocationChartConfig = null;
    }

    private async Task BtnClickGenerate()
    {
        if (Portfolio is null)
        {
            return;
        }
        Generating = true;
        ResetReport();
        CloseAlert();

        var reportType = SelectedGrouping;
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();

        var resp = await apiClient.GetReportTrendAsync(Portfolio.Id, reportType, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, SelectedCount, authToken, ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            Generating = false;
            ShowAlert("error", $"Failed to generate trend report: {resp.Message}");
            return;
        }

        var entries = resp.Data?.ToList() ?? [];
        if (entries.Count == 0)
        {
            Generating = false;
            ShowAlert("info", "No trend data is available yet. Generate the periodic reports first.");
            return;
        }

        Buckets = [.. entries.Select(MapBucket)];
        Allocation = await LoadAllocationAsync(apiClient, authToken, reportType);
        BuildCharts();
        Generating = false;
    }

    /// <summary>Maps a whole-portfolio aggregate report entry into a trend bucket (NAV-based, reconciling cash flow).</summary>
    private static TrendBucket MapBucket(ReportResp e)
    {
        var m = e.Metadata ?? new ReportEntityMetadata();

        var openValue = m.OpenValue ?? 0m;
        var closeValue = m.CloseValue ?? 0m;
        // NAV includes the running cash balance. The opening NAV uses the prior period's cash balance,
        // which is the current accumulated cash less this period's own cash movement.
        var closeNav = closeValue + m.AccumulatedCash;
        var openNav = openValue + (m.AccumulatedCash - m.Cash);

        return new TrendBucket
        {
            Label = ShortLabel(e.PeriodLabel, e.PeriodStart),
            PeriodStart = e.PeriodStart,
            OpeningValue = openNav,
            Value = closeNav,
            NetPnl = PeriodPnl(m),
            NetCashFlow = (m.Cashin ?? 0m) - (m.Cashout ?? 0m),
        };
    }

    /// <summary>Fetches the most recent period's per-symbol snapshot and derives the current NAV allocation (incl. a cash slice).</summary>
    private async Task<IReadOnlyList<AllocationSlice>> LoadAllocationAsync(IPortfolioApiClient apiClient, string authToken, ReportType reportType)
    {
        var latestStart = Buckets.Count > 0 ? Buckets[^1].PeriodStart : string.Empty;
        if (Portfolio is null || string.IsNullOrEmpty(latestStart))
        {
            return [];
        }

        var resp = await apiClient.GetReportSnapshotAsync(Portfolio.Id, reportType, latestStart, "*", authToken, ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            return [];
        }
        var entries = resp.Data?.ToList() ?? [];

        var items = entries
            .Where(e => !string.Equals(e.ItemCode, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, StringComparison.Ordinal))
            .Select(e => (Symbol: e.ItemCode, Value: e.Metadata?.CloseValue ?? 0m))
            .Where(x => x.Value > 0m)
            .OrderByDescending(x => x.Value)
            .ToList();

        var aggregate = entries.FirstOrDefault(e => string.Equals(e.ItemCode, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, StringComparison.Ordinal));
        var cash = aggregate.Metadata?.AccumulatedCash ?? 0m;

        var nav = items.Sum(x => x.Value) + (cash > 0m ? cash : 0m);
        if (nav <= 0m)
        {
            return [];
        }

        var slices = items
            .Select(x => new AllocationSlice { Symbol = ResolveSymbolName(x.Symbol), Weight = x.Value / nav })
            .ToList();
        if (cash > 0m)
        {
            slices.Add(new AllocationSlice { Symbol = "Cash", Weight = cash / nav });
        }
        return slices;
    }

    /// <summary>Resolves a friendly symbol code, stripping the exchange prefix for a compact chart label.</summary>
    private static string ResolveSymbolName(string itemCode)
    {
        var idx = itemCode.IndexOf(':');
        return idx >= 0 && idx < itemCode.Length - 1 ? itemCode[(idx + 1)..] : itemCode;
    }

    /// <summary>Period equity profit/loss (mirrors the Snapshot report's formula).</summary>
    private static decimal PeriodPnl(ReportEntityMetadata m)
        => (m.CloseValue ?? 0m) - (m.OpenValue ?? 0m)
           - m.Cost
           + (m.Dividends ?? 0m) + (m.Distributions ?? 0m) + (m.Interest ?? 0m)
           - (m.Tax ?? 0m) - (m.Fees ?? 0m);

    /// <summary>Extracts a compact period label; falls back to the period start date.</summary>
    private static string ShortLabel(string periodLabel, string periodStart)
    {
        if (string.IsNullOrEmpty(periodLabel))
        {
            return periodStart;
        }
        // Server labels look like "FY2024-25-W01: 2024-07-01 to 2024-07-07"; keep the part before the colon.
        var idx = periodLabel.IndexOf(':');
        return idx > 0 ? periodLabel[..idx].Trim() : periodLabel;
    }

    private static int DefaultCount(ReportType type) => type switch
    {
        ReportType.WEEKLY => 8,
        ReportType.MONTHLY => 6,
        ReportType.QUARTERLY => 4,
        ReportType.YEARLY => 3,
        _ => 12,
    };

    // Selectable number of periods, adapting to the report type:
    // Weekly up to 52, Monthly up to 12, Quarterly up to 8, Yearly up to 5.
    private static int[] CountOptions(ReportType type) => type switch
    {
        ReportType.WEEKLY => [4, 8, 12, 26, 53],
        ReportType.MONTHLY => [3, 6, 9, 12, 24],
        ReportType.QUARTERLY => [2, 4, 6, 8, 16],
        ReportType.YEARLY => [2, 3, 4, 5, 10],
        _ => [12],
    };

    private static string Money(decimal value, string currency)
        => $"{FormatUtils.FormatValueMaxDecimals(value, 2)} {currency}";

    private static string Percent(decimal fraction)
        => $"{FormatUtils.FormatValueMaxDecimals(fraction * 100, 2)}%";

    private static readonly string[] Palette =
    {
        "#321fdb", "#2eb85c", "#f9b115", "#e55353", "#3399ff", "#6f42c1",
        "#20c997", "#fd7e14", "#d63384", "#0dcaf0", "#6610f2", "#a0a0a0",
    };

    // ---------------------------------------------------------------------
    // Chart.js configuration builders (keys are camelCase, matching Chart.js).
    // ---------------------------------------------------------------------

    private void BuildCharts()
    {
        var labels = Buckets.Select(b => b.Label).ToArray();

        ValueChartConfig = new
        {
            type = "line",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = $"Net asset value ({Currency})",
                        data = Buckets.Select(b => b.Value).ToArray(),
                        borderColor = "#321fdb",
                        backgroundColor = "rgba(50, 31, 219, 0.1)",
                        fill = true,
                        tension = 0.3,
                        pointRadius = 2,
                    },
                },
            },
            options = LineBarOptions(),
        };

        PnlChartConfig = new
        {
            type = "bar",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = $"Net P&L ({Currency})",
                        data = Buckets.Select(b => b.NetPnl).ToArray(),
                        backgroundColor = Buckets
                            .Select(b => b.NetPnl >= 0 ? "rgba(46, 184, 92, 0.75)" : "rgba(229, 83, 83, 0.75)")
                            .ToArray(),
                    },
                },
            },
            options = LineBarOptions(),
        };

        AllocationChartConfig = Allocation.Count > 0 ? new
        {
            type = "doughnut",
            data = new
            {
                labels = Allocation.Select(a => a.Symbol).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        data = Allocation.Select(a => Math.Round(a.Weight * 100, 2)).ToArray(),
                        backgroundColor = Allocation.Select((_, i) => Palette[i % Palette.Length]).ToArray(),
                    },
                },
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { position = "right" } },
            },
        } : null;
    }

    private static object LineBarOptions() => new
    {
        responsive = true,
        maintainAspectRatio = false,
        plugins = new { legend = new { display = true } },
        scales = new
        {
            x = new { grid = new { display = false } },
            y = new { beginAtZero = false },
        },
    };
}
