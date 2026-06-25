using Microsoft.AspNetCore.Components;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTrends : CBase
{
    public enum TrendGrouping
    {
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
    }

    public sealed class TrendBucket
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }   // portfolio market value at period end
        public decimal NetPnl { get; set; }  // P&L generated during the period
        public decimal OpeningValue => Value - NetPnl;
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

    private TrendGrouping SelectedGrouping { get; set; } = TrendGrouping.Monthly;
    private int SelectedCount { get; set; } = 12;

    private bool Generating { get; set; }

    private IReadOnlyList<TrendBucket> Buckets { get; set; } = [];
    private IReadOnlyList<AllocationSlice> Allocation { get; set; } = [];

    private object? ValueChartConfig { get; set; }
    private object? PnlChartConfig { get; set; }
    private object? AllocationChartConfig { get; set; }

    private string Currency => Portfolio?.Currency ?? "USD";

    private string? _loadedPortfolioId;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // Initialise once per portfolio so background re-renders don't clobber the user's selections.
        if (Portfolio is not null && !string.Equals(Portfolio.Id, _loadedPortfolioId, StringComparison.Ordinal))
        {
            _loadedPortfolioId = Portfolio.Id;
            SelectedGrouping = TrendGrouping.Monthly;
            SelectedCount = DefaultCount(SelectedGrouping);
            ResetReport();
        }
    }

    private void SelectGrouping(TrendGrouping grouping)
    {
        if (SelectedGrouping != grouping)
        {
            SelectedGrouping = grouping;
            SelectedCount = DefaultCount(grouping);
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

    private void BtnClickGenerate()
    {
        Generating = true;
        Buckets = BuildMockBuckets(SelectedGrouping, SelectedCount);
        Allocation = BuildMockAllocation();
        BuildCharts();
        Generating = false;
    }

    private static int DefaultCount(TrendGrouping grouping) => grouping switch
    {
        TrendGrouping.Weekly => 12,
        TrendGrouping.Monthly => 12,
        TrendGrouping.Quarterly => 8,
        TrendGrouping.Yearly => 5,
        _ => 12,
    };

    // Selectable number of periods, adapting to the report type:
    // Weekly up to 52, Monthly up to 12, Quarterly up to 8, Yearly up to 5.
    private static IReadOnlyList<int> CountOptions(TrendGrouping grouping) => grouping switch
    {
        TrendGrouping.Weekly => new[] { 4, 8, 12, 26, 52 },
        TrendGrouping.Monthly => new[] { 3, 6, 9, 12 },
        TrendGrouping.Quarterly => new[] { 4, 6, 8 },
        TrendGrouping.Yearly => new[] { 2, 3, 4, 5 },
        _ => new[] { 12 },
    };

    private static string GroupingTitle(TrendGrouping grouping) => grouping switch
    {
        TrendGrouping.Weekly => "Weekly",
        TrendGrouping.Monthly => "Monthly",
        TrendGrouping.Quarterly => "Quarterly",
        TrendGrouping.Yearly => "Yearly",
        _ => grouping.ToString(),
    };

    private static string Money(decimal value, string currency)
        => $"{FormatUtils.FormatValueMaxDecimals(value, 2)} {currency}";

    private static string Percent(decimal fraction)
        => $"{FormatUtils.FormatValueMaxDecimals(fraction * 100, 2)}%";

    // ---------------------------------------------------------------------
    // Mock / dummy data (to be replaced by the real reporting/aggregation engine).
    // ---------------------------------------------------------------------

    private static readonly string[] MockSymbols = { "VNM", "FPT", "VCB", "AAPL", "MSFT", "KO" };

    private static readonly string[] Palette =
    {
        "#321fdb", "#2eb85c", "#f9b115", "#e55353", "#3399ff", "#6f42c1",
    };

    private IReadOnlyList<TrendBucket> BuildMockBuckets(TrendGrouping grouping, int count)
    {
        var labels = BuildPeriodLabels(grouping, count);
        var seed = HashCode.Combine((int)grouping, count, Portfolio?.Id ?? string.Empty);
        var rnd = new Random(seed);

        var buckets = new List<TrendBucket>(labels.Count);
        var value = (decimal)((rnd.NextDouble() * 20000) + 30000);
        foreach (var label in labels)
        {
            var changePct = (decimal)((rnd.NextDouble() - 0.45) * 0.12);
            var pnl = Math.Round(value * changePct, 2);
            value = Math.Round(value + pnl, 2);
            buckets.Add(new TrendBucket { Label = label, Value = value, NetPnl = pnl });
        }
        return buckets;
    }

    private IReadOnlyList<AllocationSlice> BuildMockAllocation()
    {
        var seed = HashCode.Combine("allocation", Portfolio?.Id ?? string.Empty);
        var rnd = new Random(seed);
        var raw = MockSymbols.Select(s => (Symbol: s, Weight: rnd.NextDouble() + 0.1)).ToList();
        var total = raw.Sum(x => x.Weight);
        return raw.Select(x => new AllocationSlice { Symbol = x.Symbol, Weight = (decimal)(x.Weight / total) }).ToList();
    }

    private IReadOnlyList<string> BuildPeriodLabels(TrendGrouping grouping, int count)
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfWeek = Portfolio?.Metadata?.FirstDayOfWeek ?? DayOfWeek.Monday;
        var fiscalStartMonth = Portfolio?.Metadata?.FiscalYearStartMonth ?? 1;
        var labels = new List<string>(count);

        for (var i = count - 1; i >= 0; i--)
        {
            switch (grouping)
            {
                case TrendGrouping.Weekly:
                    var weekStart = StartOfWeek(today, firstDayOfWeek).AddDays(-7 * i);
                    labels.Add(weekStart.ToString("MMM d"));
                    break;

                case TrendGrouping.Monthly:
                    var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                    labels.Add(monthStart.ToString("MMM yy"));
                    break;

                case TrendGrouping.Quarterly:
                    var quarterStart = FiscalQuarterStart(today, fiscalStartMonth).AddMonths(-3 * i);
                    var fyStart = FiscalYearStart(quarterStart, fiscalStartMonth);
                    var quarterNum = (((quarterStart.Year - fyStart.Year) * 12) + (quarterStart.Month - fyStart.Month)) / 3 + 1;
                    labels.Add($"Q{quarterNum} '{fyStart.Year % 100:D2}");
                    break;

                case TrendGrouping.Yearly:
                    var yearStart = FiscalYearStart(today, fiscalStartMonth).AddYears(-i);
                    labels.Add($"FY{yearStart.Year}");
                    break;
            }
        }

        return labels;
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - firstDayOfWeek)) % 7;
        return date.AddDays(-diff);
    }

    private static DateTime FiscalYearStart(DateTime date, int fiscalStartMonth)
    {
        var year = date.Month >= fiscalStartMonth ? date.Year : date.Year - 1;
        return new DateTime(year, fiscalStartMonth, 1);
    }

    private static DateTime FiscalQuarterStart(DateTime date, int fiscalStartMonth)
    {
        var fyStart = FiscalYearStart(date, fiscalStartMonth);
        var monthsSince = ((date.Year - fyStart.Year) * 12) + (date.Month - fyStart.Month);
        return fyStart.AddMonths((monthsSince / 3) * 3);
    }

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
                        label = $"Portfolio value ({Currency})",
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

        AllocationChartConfig = new
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
                        backgroundColor = Palette,
                    },
                },
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { position = "right" } },
            },
        };
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
