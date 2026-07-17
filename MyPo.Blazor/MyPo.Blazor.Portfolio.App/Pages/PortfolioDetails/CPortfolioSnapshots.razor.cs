using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioSnapshots : CBase
{
    /// <summary>A single symbol's position snapshot for the selected period.</summary>
    public sealed class SnapshotRow
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Holdings { get; set; }
        public decimal OpenValue { get; set; }
        public decimal CloseValue { get; set; }
        public decimal PeriodPnl { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal Dividends { get; set; }
        public decimal Distributions { get; set; }
        public decimal Interest { get; set; }
        /// <summary>Accumulated (lifetime-to-period-end) dividends.</summary>
        public decimal AccumulatedDividends { get; set; }
        /// <summary>Accumulated (lifetime-to-period-end) distributions.</summary>
        public decimal AccumulatedDistributions { get; set; }
        public decimal Fees { get; set; }
        public decimal Tax { get; set; }
        public decimal Buys { get; set; }
        public decimal Sells { get; set; }
        /// <summary>Share of the portfolio's total close value (%).</summary>
        public decimal Weight { get; set; }
        public bool IsFinal { get; set; }
    }

    /// <summary>The entire-portfolio ("*") snapshot summary for the selected period.</summary>
    public sealed class PortfolioSnapshot
    {
        public decimal OpenValue { get; set; }
        public decimal CloseValue { get; set; }
        public decimal PeriodPnl { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal Dividends { get; set; }
        public decimal Distributions { get; set; }
        public decimal Interest { get; set; }
        public decimal Fees { get; set; }
        public decimal Tax { get; set; }
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }

        /// <summary>Period net cash flow for the portfolio.</summary>
        public decimal Cash { get; set; }

        /// <summary>Running cash balance held by the portfolio at the period close.</summary>
        public decimal AccumulatedCash { get; set; }

        /// <summary>Net asset value = holdings (close) value plus the cash balance.</summary>
        public decimal Nav => CloseValue + AccumulatedCash;

        public bool IsFinal { get; set; }
    }

    public sealed class PortfolioReport
    {
        public ReportType Type { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime GeneratedUtc { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsFinal { get; set; }

        /// <summary>The whole-portfolio ("*") snapshot; null for a single-symbol report.</summary>
        public PortfolioSnapshot? Portfolio { get; set; }

        /// <summary>Per-symbol position snapshots.</summary>
        public IReadOnlyList<SnapshotRow> Items { get; set; } = [];

        /// <summary>Chart.js configs for the entire-portfolio charts (null when there is nothing to plot).</summary>
        public object? PeriodPnlChartConfig { get; set; }
        public object? TotalReturnChartConfig { get; set; }
        public object? IncomeChartConfig { get; set; }
        public object? IncomeYieldChartConfig { get; set; }

        public bool HasPortfolio => Portfolio is not null;
        public bool HasItems => Items.Count > 0;
        public bool HasCharts => PeriodPnlChartConfig is not null
            || TotalReturnChartConfig is not null || IncomeChartConfig is not null || IncomeYieldChartConfig is not null;
    }

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private MarketDefResp? Market = null;

    private ReportType SelectedType { get; set; } = ReportType.MONTHLY; // default report is MONTHLY

    private IReadOnlyList<ReportPeriod> AvailablePeriods { get; set; } = [];
    private string SelectedPeriodKey { get; set; } = string.Empty;

    // Report periods rarely change, so they are fetched once per portfolio (for every report type)
    // and cached here; switching report type then just reads from this cache.
    private readonly Dictionary<ReportType, IReadOnlyList<ReportPeriod>> _periodsByType = new();

    private IReadOnlyList<AssetResp> Symbols { get; set; } = [];

    // Empty string => report on the entire portfolio.
    private string SelectedSymbol { get; set; } = string.Empty;

    private PortfolioReport? CurrentReport { get; set; }
    private bool Generating { get; set; }

    private string? _loadedPortfolioId;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        // Initialise once per portfolio so background re-renders don't clobber the user's selections.
        if (Portfolio is not null && !string.Equals(Portfolio.Id, _loadedPortfolioId, StringComparison.OrdinalIgnoreCase))
        {
            _loadedPortfolioId = Portfolio.Id;
            Market = Markets?.FirstOrDefault(m => string.Equals(m.Id, Portfolio.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));
            CurrentReport = null;
            _ = await LoadSymbolsAsync() && await LoadAllPeriodsAsync();
        }
    }

    /// <summary>Loads the portfolio's holdings from the server to populate the symbol selector.</summary>
    private async Task<bool> LoadSymbolsAsync()
    {
        Symbols = [];
        if (Portfolio is null)
        {
            return true;
        }
        ShowAlert("info", "Loading portfolio symbols...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();
        var resp = await apiClient.GetMyPortfolioAssetsAsync(Portfolio.Id, authToken, ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            ShowAlert("error", $"Failed to load portfolio symbols: {resp.Message}");
            return false;
        }

        // Resolve markets so each holding's item code can be normalized to the EXCHANGE:SYMBOL form
        // (e.g. NASDAQ:AAPL) that the report entries use. This keeps the symbol selector values aligned
        // with the report snapshot rows so a per-symbol report can be matched on the server.
        var marketsResp = await apiClient.GetMarketsAsync(authToken, ApiBaseUrl);
        if (!marketsResp.IsSuccess)
        {
            ShowAlert("error", $"Failed to load markets: {marketsResp.Message}");
            return false;
        }
        var marketCodeById = marketsResp.Data?.ToDictionary(m => m.Id, m => m.Code) ?? new();

        var assets = resp.Data?.ToList() ?? [];
        foreach (var asset in assets)
        {
            var marketCode = asset.MarketId is not null && marketCodeById.TryGetValue(asset.MarketId, out var code) ? code : null;
            asset.ItemCode = $"{(string.IsNullOrEmpty(marketCode) ? string.Empty : marketCode + ":")}{asset.ItemCode}".ToUpper();
        }

        Symbols = [.. assets.OrderBy(a => a.ItemCode, StringComparer.OrdinalIgnoreCase)];
        CloseAlert();
        return true;
    }

    /// <summary>
    /// Fetches the available report periods for every report type from the server, once per portfolio,
    /// and caches them. Periods rarely change so this avoids a server round-trip on every type switch.
    /// </summary>
    private async Task<bool> LoadAllPeriodsAsync()
    {
        _periodsByType.Clear();
        AvailablePeriods = [];
        SelectedPeriodKey = string.Empty;
        if (Portfolio is null)
        {
            return true;
        }
        ShowAlert("info", "Loading report periods...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();
        foreach (var type in Enum.GetValues<ReportType>())
        {
            var resp = await apiClient.GetReportPeriodsAsync(Portfolio.Id, type, authToken, ApiBaseUrl);
            if (!resp.IsSuccess)
            {
                ShowAlert("error", $"Failed to load '{type}' report periods: {resp.Message}");
                return false;
            }
            _periodsByType[type] = resp.Data?.ToList() ?? [];
        }
        ApplyPeriodsForSelectedType();
        CloseAlert();
        return true;
    }

    private void SelectType(ReportType type)
    {
        if (SelectedType != type)
        {
            SelectedType = type;
            // The displayed report is now stale; require an explicit Generate.
            CurrentReport = null;
            // Periods are already cached; just switch to the selected type's list.
            ApplyPeriodsForSelectedType();
        }
    }

    private async Task OnPeriodChanged(ChangeEventArgs e)
    {
        SelectedPeriodKey = e.Value?.ToString() ?? string.Empty;
        // Changing the period auto-loads the report (no explicit Generate click needed).
        await BtnClickGenerate();
    }

    private async Task OnSymbolChanged(ChangeEventArgs e)
    {
        SelectedSymbol = e.Value?.ToString() ?? string.Empty;
        // Changing the symbol auto-loads the report (no explicit Generate click needed).
        await BtnClickGenerate();
    }

    /// <summary>Applies the cached period list for the currently selected report type.</summary>
    private void ApplyPeriodsForSelectedType()
    {
        AvailablePeriods = _periodsByType.TryGetValue(SelectedType, out var periods) ? periods : [];
        SelectedPeriodKey = AvailablePeriods.FirstOrDefault()?.Start ?? string.Empty;
    }

    private async Task BtnClickGenerate()
    {
        if (Portfolio is null)
        {
            return;
        }
        if (string.IsNullOrEmpty(SelectedPeriodKey))
        {
            ShowAlert("warning", "Please select a report period first.");
            return;
        }
        Generating = true;
        CurrentReport = null;
        CloseAlert();

        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.GetReportSnapshotAsync(
            Portfolio.Id, SelectedType, SelectedPeriodKey, SelectedSymbol,
            await GetAuthTokenAsync(), ApiBaseUrl);
        Generating = false;

        if (!resp.IsSuccess)
        {
            ShowAlert("error", $"Failed to generate report: {resp.Message}");
            return;
        }

        var entries = resp.Data?.ToList() ?? [];
        if (entries.Count == 0)
        {
            ShowAlert("info", "No report data is available for the selected period yet.");
            return;
        }

        CurrentReport = BuildReport(SelectedType, SelectedPeriodKey, SelectedSymbol, entries);
    }

    /// <summary>Maps the report snapshot rows fetched from the server into the view model.</summary>
    private PortfolioReport BuildReport(ReportType type, string periodStart, string symbol, IReadOnlyList<ReportResp> entries)
    {
        var currency = Market?.CurrencySymbol ?? Portfolio?.Currency ?? "USD";

        // Prefer the label the server stored on the report rows; fall back to the selected period's label.
        var periodLabel = entries.Select(e => e.PeriodLabel).FirstOrDefault(l => !string.IsNullOrEmpty(l))
            ?? AvailablePeriods.FirstOrDefault(p => p.Start == periodStart)?.Label
            ?? periodStart;

        // The whole-portfolio ("*") row is the aggregate snapshot; the rest are per-symbol snapshots.
        // The aggregate row is only present for an entire-portfolio query; a single-symbol query returns just that symbol's rows.
        var hasPortfolioRow = entries.Any(e => string.Equals(e.ItemCode, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, StringComparison.Ordinal));
        var portfolioEntry = entries.FirstOrDefault(e => string.Equals(e.ItemCode, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, StringComparison.Ordinal));
        var itemEntries = entries.Where(e => !string.Equals(e.ItemCode, ReportEntity.ITEM_CODE_ENTIRE_PORTFOLIO, StringComparison.Ordinal)).ToList();

        // Portfolio close value drives the per-symbol weights; fall back to the sum of item values.
        var portfolioCloseValue = portfolioEntry.Metadata?.CloseValue
            ?? itemEntries.Sum(e => e.Metadata?.CloseValue ?? 0m);

        SnapshotRow MapItem(ReportResp e)
        {
            var m = e.Metadata ?? new ReportEntityMetadata();
            var close = m.CloseValue ?? 0m;
            return new SnapshotRow
            {
                ItemCode = e.ItemCode,
                Name = ResolveSymbolName(e.ItemCode),
                Holdings = m.AccumulatedQuantity ?? 0m,
                OpenValue = m.OpenValue ?? 0m,
                CloseValue = close,
                PeriodPnl = PeriodPnl(m),
                TotalReturn = TotalReturn(m),
                Dividends = m.Dividends ?? 0m,
                Distributions = m.Distributions ?? 0m,
                Interest = m.Interest ?? 0m,
                AccumulatedDividends = m.AccumulatedDividends ?? 0m,
                AccumulatedDistributions = m.AccumulatedDistributions ?? 0m,
                Fees = m.Fees ?? 0m,
                Tax = m.Tax ?? 0m,
                Buys = m.Buys ?? 0m,
                Sells = m.Sells ?? 0m,
                Weight = portfolioCloseValue != 0 ? close / portfolioCloseValue * 100m : 0m,
                IsFinal = e.IsFinal,
            };
        }

        var items = itemEntries
            .OrderByDescending(e => e.Metadata?.CloseValue ?? 0m)
            .ThenBy(e => e.ItemCode, StringComparer.OrdinalIgnoreCase)
            .Select(MapItem)
            .ToList();

        var m = portfolioEntry.Metadata ?? new ReportEntityMetadata();
        PortfolioSnapshot portfolioSnapshot = new PortfolioSnapshot
        {
            OpenValue = m.OpenValue ?? 0m,
            CloseValue = m.CloseValue ?? 0m,
            PeriodPnl = PeriodPnl(m),
            TotalReturn = TotalReturn(m),
            Dividends = m.AccumulatedDividends ?? 0m,
            Distributions = m.AccumulatedDistributions ?? 0m,
            Interest = m.AccumulatedInterest ?? 0m,
            Fees = m.AccumulatedFees ?? 0m,
            Tax = m.AccumulatedTax ?? 0m,
            CashIn = m.AccumulatedCashin ?? 0m,
            CashOut = m.AccumulatedCashout ?? 0m,
            Cash = m.Cash,
            AccumulatedCash = m.AccumulatedCash,
            IsFinal = portfolioEntry.IsFinal,
        };

        var report = new PortfolioReport
        {
            Type = type,
            PeriodLabel = periodLabel,
            Scope = string.IsNullOrEmpty(symbol) || symbol == "*" ? "Entire portfolio" : symbol,
            GeneratedUtc = DateTime.UtcNow,
            Currency = currency,
            // Only expose the aggregate snapshot for an entire-portfolio query; otherwise render the single-symbol detail.
            Portfolio = hasPortfolioRow ? portfolioSnapshot : null,
            Items = items,
            IsFinal = (hasPortfolioRow ? portfolioSnapshot.IsFinal : true) && items.All(i => i.IsFinal) && (hasPortfolioRow || items.Count > 0),
        };
        report.PeriodPnlChartConfig = BuildPeriodPnlChartConfig(report);
        report.TotalReturnChartConfig = BuildTotalReturnChartConfig(report);
        report.IncomeChartConfig = BuildIncomeChartConfig(report);
        report.IncomeYieldChartConfig = BuildIncomeYieldChartConfig(report);
        return report;
    }

    /// <summary>Period profit/loss: value change over the period adjusted for the period's own cash flows.</summary>
    private static decimal PeriodPnl(ReportEntityMetadata m)
        => (m.CloseValue ?? 0m) - (m.OpenValue ?? 0m)
           - (m.Cost/* ?? 0m*/)     // Cost = buys - sells
           + (m.Dividends ?? 0m) + (m.Distributions ?? 0m) + (m.Interest ?? 0m)
           - (m.Tax ?? 0m) - (m.Fees ?? 0m);

    /// <summary>Total return since inception: current value less net invested plus accumulated income and costs.</summary>
    private static decimal TotalReturn(ReportEntityMetadata m)
        => (m.CloseValue ?? 0m) - (m.AccumulatedCost/* ?? 0m*/)     // AccumulatedCost = accumulated buys - accumulated sells
           + (m.AccumulatedDividends ?? 0m) + (m.AccumulatedDistributions ?? 0m) + (m.AccumulatedInterest ?? 0m)
           - (m.AccumulatedTax ?? 0m) - (m.AccumulatedFees ?? 0m);

    // Semi-transparent fills with solid borders for signed (gain/loss) bars.
    private const string GainFill = "rgba(46, 184, 92, 0.65)";
    private const string LossFill = "rgba(229, 83, 83, 0.65)";
    private const string GainBorder = "#2eb85c";
    private const string LossBorder = "#e55353";

    /// <summary>Horizontal bar of each symbol's period P&L (green gains / red losses), sorted best-first.</summary>
    private static object? BuildPeriodPnlChartConfig(PortfolioReport report)
    {
        var rows = report.Items.Where(i => i.PeriodPnl != 0m).OrderByDescending(i => i.PeriodPnl).ToList();
        if (rows.Count == 0)
        {
            return null;
        }
        return new
        {
            type = "bar",
            data = new
            {
                labels = rows.Select(r => r.ItemCode).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = $"Period P&L ({report.Currency})",
                        data = rows.Select(r => r.PeriodPnl).ToArray(),
                        backgroundColor = rows.Select(r => r.PeriodPnl >= 0 ? GainFill : LossFill).ToArray(),
                        borderColor = rows.Select(r => r.PeriodPnl >= 0 ? GainBorder : LossBorder).ToArray(),
                        borderWidth = 1,
                    },
                },
            },
            options = new
            {
                indexAxis = "y",
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { display = false } },
                scales = new
                {
                    x = new { beginAtZero = true },
                    y = new { grid = new { display = false } },
                },
            },
        };
    }

    /// <summary>Horizontal bar of each symbol's total return since inception (green gains / red losses), sorted best-first.</summary>
    private static object? BuildTotalReturnChartConfig(PortfolioReport report)
    {
        var rows = report.Items.Where(i => i.TotalReturn != 0m).OrderByDescending(i => i.TotalReturn).ToList();
        if (rows.Count == 0)
        {
            return null;
        }
        return new
        {
            type = "bar",
            data = new
            {
                labels = rows.Select(r => r.ItemCode).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = $"Total return ({report.Currency})",
                        data = rows.Select(r => r.TotalReturn).ToArray(),
                        backgroundColor = rows.Select(r => r.TotalReturn >= 0 ? GainFill : LossFill).ToArray(),
                        borderColor = rows.Select(r => r.TotalReturn >= 0 ? GainBorder : LossBorder).ToArray(),
                        borderWidth = 1,
                    },
                },
            },
            options = new
            {
                indexAxis = "y",
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { display = false } },
                scales = new
                {
                    x = new { beginAtZero = true },
                    y = new { grid = new { display = false } },
                },
            },
        };
    }

    /// <summary>
    /// Stacked bar of each symbol's accumulated (lifetime-to-period-end) income - dividends and distributions,
    /// sorted by total income descending. Interest is excluded as it is booked at portfolio (cash) level, not per symbol.
    /// </summary>
    private static object? BuildIncomeChartConfig(PortfolioReport report)
    {
        var rows = report.Items
            .Where(i => i.AccumulatedDividends != 0m || i.AccumulatedDistributions != 0m)
            .OrderByDescending(i => i.AccumulatedDividends + i.AccumulatedDistributions)
            .ToList();
        if (rows.Count == 0)
        {
            return null;
        }
        var labels = rows.Select(r => r.ItemCode).ToArray();
        return new
        {
            type = "bar",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new { label = "Dividends", data = rows.Select(r => r.AccumulatedDividends).ToArray(), backgroundColor = "rgba(50, 31, 219, 0.65)", stack = "income" },
                    new { label = "Distributions", data = rows.Select(r => r.AccumulatedDistributions).ToArray(), backgroundColor = "rgba(32, 201, 151, 0.65)", stack = "income" },
                },
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { position = "top" } },
                scales = new
                {
                    x = new { stacked = true, grid = new { display = false } },
                    y = new { stacked = true, beginAtZero = true },
                },
            },
        };
    }

    /// <summary>
    /// Horizontal bar of each symbol's income yield: accumulated income (dividends + distributions) as a percentage
    /// of its current close value, sorted descending. Shows how income-productive each holding is relative to its size.
    /// </summary>
    private static object? BuildIncomeYieldChartConfig(PortfolioReport report)
    {
        var rows = report.Items
            .Where(i => i.CloseValue > 0 && (i.AccumulatedDividends + i.AccumulatedDistributions) != 0m)
            .Select(i => new { i.ItemCode, Yield = (i.AccumulatedDividends + i.AccumulatedDistributions) / i.CloseValue * 100m })
            .OrderByDescending(x => x.Yield)
            .ToList();
        if (rows.Count == 0)
        {
            return null;
        }
        return new
        {
            type = "bar",
            data = new
            {
                labels = rows.Select(r => r.ItemCode).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Income yield (% of value)",
                        data = rows.Select(r => r.Yield).ToArray(),
                        backgroundColor = "rgba(32, 201, 151, 0.65)",
                        borderColor = "#20c997",
                        borderWidth = 1,
                    },
                },
            },
            options = new
            {
                indexAxis = "y",
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { display = false } },
                scales = new
                {
                    x = new { beginAtZero = true },
                    y = new { grid = new { display = false } },
                },
            },
        };
    }

    /// <summary>Bootstrap text color class for a signed value (gain/loss/neutral).</summary>
    private static string PnlClass(decimal value)
        => value > 0 ? "text-success" : value < 0 ? "text-danger" : "text-muted";

    /// <summary>Resolves a friendly name for an item code, using the loaded portfolio symbols when available.</summary>
    private string ResolveSymbolName(string itemCode)
    {
        if (string.Equals(itemCode, "*", StringComparison.Ordinal))
        {
            return "Entire portfolio";
        }
        var asset = Symbols.FirstOrDefault(a => string.Equals(a.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase));
        return asset?.Metadata?.CorpName ?? itemCode;
    }

    private static string Money(decimal value, string currency)
        => $"{FormatUtils.FormatValueMaxDecimals(value, 2)} {currency}";
}
