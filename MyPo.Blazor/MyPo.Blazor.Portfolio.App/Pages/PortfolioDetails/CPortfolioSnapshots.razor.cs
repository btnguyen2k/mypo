using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioSnapshots : CBase
{
    public sealed class ReportItemRow
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TxType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public bool IsFinal { get; set; }
    }

    public sealed class PortfolioReport
    {
        public ReportType Type { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime GeneratedUtc { get; set; }
        public string Currency { get; set; } = string.Empty;

        /// <summary>Per-symbol entries (ItemCode in EXCHANGE:SYMBOL form); rendered as a table.</summary>
        public IReadOnlyList<ReportItemRow> ItemRows { get; set; } = [];

        /// <summary>Aggregated whole-portfolio entries (ItemCode == "*"); rendered as a chart.</summary>
        public IReadOnlyList<ReportItemRow> AggregateRows { get; set; } = [];

        /// <summary>Chart.js configuration for the aggregate section, or null when there is no aggregate.</summary>
        public object? AggregateChartConfig { get; set; }

        public bool HasItems => ItemRows.Count > 0;
        public bool HasAggregate => AggregateRows.Count > 0;

        public bool IsFinal => (AggregateRows.Count > 0 || ItemRows.Count > 0)
            && AggregateRows.Concat(ItemRows).All(r => r.IsFinal);
    }

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private ReportType SelectedType { get; set; } = ReportType.WEEKLY;

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
        var resp = await apiClient.GetMyPortfolioAssetsAsync(Portfolio.Id, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            ShowAlert("error", $"Failed to load portfolio symbols: {resp.Message}");
            return false;
        }
        Symbols = resp.Data?
            .OrderBy(a => a.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
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

    private void OnPeriodChanged(ChangeEventArgs e)
    {
        SelectedPeriodKey = e.Value?.ToString() ?? string.Empty;
        CurrentReport = null;
    }

    private void OnSymbolChanged(ChangeEventArgs e)
    {
        SelectedSymbol = e.Value?.ToString() ?? string.Empty;
        CurrentReport = null;
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
    private PortfolioReport BuildReport(ReportType type, string periodStart, string symbol, IReadOnlyList<ReportEntity> entries)
    {
        var currency = Portfolio?.Currency ?? "USD";

        // Prefer the label the server stored on the report rows; fall back to the selected period's label.
        var periodLabel = entries.Select(e => e.PeriodLabel).FirstOrDefault(l => !string.IsNullOrEmpty(l))
            ?? AvailablePeriods.FirstOrDefault(p => p.Start == periodStart)?.Label
            ?? periodStart;

        ReportItemRow Map(ReportEntity e) => new()
        {
            ItemCode = e.ItemCode,
            Name = ResolveSymbolName(e.ItemCode),
            TxType = e.TxType,
            Quantity = e.Metadata?.Quantity ?? 0,
            Cost = e.Metadata?.Cost ?? 0,
            IsFinal = e.IsFinal,
        };

        // Per-symbol entries (EXCHANGE:SYMBOL) feed the table; the aggregate ("*") entries feed the chart.
        var itemRows = entries
            .Where(e => !string.Equals(e.ItemCode, "*", StringComparison.Ordinal))
            .OrderBy(e => e.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.TxType, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
        var aggregateRows = entries
            .Where(e => string.Equals(e.ItemCode, "*", StringComparison.Ordinal))
            .OrderBy(e => e.TxType, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();

        var report = new PortfolioReport
        {
            Type = type,
            PeriodLabel = periodLabel,
            Scope = string.IsNullOrEmpty(symbol) ? "Entire portfolio" : symbol,
            GeneratedUtc = DateTime.UtcNow,
            Currency = currency,
            ItemRows = itemRows,
            AggregateRows = aggregateRows,
        };
        report.AggregateChartConfig = BuildAggregateChartConfig(report);
        return report;
    }

    /// <summary>
    /// Builds a Chart.js bar-chart configuration summarizing the aggregated portfolio value by transaction type.
    /// Returns null when there is no aggregate ("*") data to plot.
    /// </summary>
    private static object? BuildAggregateChartConfig(PortfolioReport report)
    {
        if (report.AggregateRows.Count == 0)
        {
            return null;
        }
        var labels = report.AggregateRows.Select(r => r.TxType).ToArray();
        var data = report.AggregateRows.Select(r => r.Cost).ToArray();
        return new
        {
            type = "bar",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = $"Value by transaction type ({report.Currency})",
                        data,
                        backgroundColor = "rgba(50, 31, 219, 0.65)",
                        borderColor = "#321fdb",
                        borderWidth = 1,
                    },
                },
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                plugins = new { legend = new { display = false } },
                scales = new
                {
                    x = new { grid = new { display = false } },
                    y = new { beginAtZero = true },
                },
            },
        };
    }

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
