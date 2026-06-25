using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioSnapshots : CBase
{
    public sealed class ReportHoldingRow
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal OpenValue { get; set; }
        public decimal CloseValue { get; set; }
        public decimal Pnl => CloseValue - OpenValue;
        public decimal PnlPct => OpenValue != 0 ? Pnl / OpenValue : 0;
    }

    public sealed class PortfolioReport
    {
        public ReportType Type { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime GeneratedUtc { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal OpeningValue { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal NetPnl => ClosingValue - OpeningValue;
        public decimal NetPnlPct => OpeningValue != 0 ? NetPnl / OpeningValue : 0;
        public IReadOnlyList<ReportHoldingRow> Holdings { get; set; } = [];
        public string Narrative { get; set; } = string.Empty;
    }

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private ReportType SelectedType { get; set; } = ReportType.WEEKLY;

    private IReadOnlyList<string> AvailablePeriods { get; set; } = [];
    private string SelectedPeriodKey { get; set; } = string.Empty;

    // Report periods rarely change, so they are fetched once per portfolio (for every report type)
    // and cached here; switching report type then just reads from this cache.
    private readonly Dictionary<ReportType, IReadOnlyList<string>> _periodsByType = new();

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
        ShowAlert("info", "Loading portfolio symbols from server...");
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
        ShowAlert("info", "Loading report periods from server...");
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
        SelectedPeriodKey = AvailablePeriods.FirstOrDefault() ?? string.Empty;
    }

    private void BtnClickGenerate()
    {
        if (string.IsNullOrEmpty(SelectedPeriodKey))
        {
            ShowAlert("warning", "Please select a report period first.");
            return;
        }
        Generating = true;
        CloseAlert();
        CurrentReport = BuildMockReport(SelectedType, SelectedPeriodKey, SelectedSymbol);
        Generating = false;
    }

    // ---------------------------------------------------------------------
    // Mock / dummy data generators (to be replaced by the real reporting engine).
    // ---------------------------------------------------------------------

    private static readonly (string Symbol, string Name)[] MockUniverse =
    {
        ("HOSE:VNM", "Vietnam Dairy Products"),
        ("HOSE:FPT", "FPT Corporation"),
        ("HOSE:VCB", "Vietcombank"),
        ("NASDAQ:AAPL", "Apple Inc."),
        ("NASDAQ:MSFT", "Microsoft Corp."),
        ("NYSE:KO", "The Coca-Cola Company"),
    };

    private PortfolioReport BuildMockReport(ReportType type, string period, string symbol)
    {
        var currency = Portfolio?.Currency ?? "USD";
        var seed = HashCode.Combine((int)type, period, symbol, Portfolio?.Id ?? string.Empty);
        var rnd = new Random(seed);

        // Use the portfolio's real holdings when available; fall back to a demo universe otherwise.
        var portfolioUniverse = Symbols.Count > 0
            ? Symbols.Select(a => (Symbol: a.ItemCode, Name: a.Metadata?.CorpName ?? a.ItemCode)).ToArray()
            : MockUniverse;
        var universe = string.IsNullOrEmpty(symbol)
            ? portfolioUniverse
            : portfolioUniverse
                .Where(x => string.Equals(x.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                .DefaultIfEmpty((Symbol: symbol, Name: symbol))
                .ToArray();

        var holdings = universe.Select(x =>
        {
            var open = Math.Round((decimal)(rnd.NextDouble() * 9000 + 1000), 2);
            // change in range roughly -9% .. +11%
            var change = (decimal)((rnd.NextDouble() - 0.45) * 0.2);
            var close = Math.Round(open * (1 + change), 2);
            return new ReportHoldingRow
            {
                Symbol = x.Symbol,
                Name = x.Name,
                OpenValue = open,
                CloseValue = close,
            };
        }).ToList();

        var report = new PortfolioReport
        {
            Type = type,
            PeriodLabel = period,
            Scope = string.IsNullOrEmpty(symbol) ? "Entire portfolio" : symbol,
            GeneratedUtc = DateTime.UtcNow,
            Currency = currency,
            OpeningValue = holdings.Sum(h => h.OpenValue),
            ClosingValue = holdings.Sum(h => h.CloseValue),
            Holdings = holdings,
        };
        report.Narrative = BuildMockNarrative(report);
        return report;
    }

    private static string Money(decimal value, string currency)
        => $"{FormatUtils.FormatValueMaxDecimals(value, 2)} {currency}";

    private static string Percent(decimal fraction)
        => $"{FormatUtils.FormatValueMaxDecimals(fraction * 100, 2)}%";

    private static string BuildMockNarrative(PortfolioReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("> ⚠️ _Sample data — the reporting engine has not been implemented yet._");
        sb.AppendLine();
        sb.AppendLine($"**{TypeTitle(report.Type)} report** for **{report.Scope}** covering **{report.PeriodLabel}**.");
        sb.AppendLine();

        var best = report.Holdings.OrderByDescending(h => h.PnlPct).FirstOrDefault();
        var worst = report.Holdings.OrderBy(h => h.PnlPct).FirstOrDefault();

        sb.AppendLine("### Highlights");
        sb.AppendLine();
        sb.AppendLine($"- Opening value: **{Money(report.OpeningValue, report.Currency)}**");
        sb.AppendLine($"- Closing value: **{Money(report.ClosingValue, report.Currency)}**");
        sb.AppendLine($"- Net P&L: **{Money(report.NetPnl, report.Currency)}** ({Percent(report.NetPnlPct)})");
        if (best is not null)
        {
            sb.AppendLine($"- 🟢 Top performer: **{best.Symbol}** ({Percent(best.PnlPct)})");
        }
        if (worst is not null && report.Holdings.Count > 1)
        {
            sb.AppendLine($"- 🔴 Laggard: **{worst.Symbol}** ({Percent(worst.PnlPct)})");
        }
        sb.AppendLine();

        sb.AppendLine("### Holdings breakdown");
        sb.AppendLine();
        sb.AppendLine("| Symbol | Name | Open | Close | P&L | P&L % |");
        sb.AppendLine("|---|---|--:|--:|--:|--:|");
        foreach (var h in report.Holdings.OrderByDescending(h => h.PnlPct))
        {
            sb.AppendLine($"| `{h.Symbol}` | {h.Name} | {Money(h.OpenValue, report.Currency)} | {Money(h.CloseValue, report.Currency)} | {Money(h.Pnl, report.Currency)} | {Percent(h.PnlPct)} |");
        }
        sb.AppendLine();
        sb.AppendLine($"_Generated at {report.GeneratedUtc:yyyy-MM-dd HH:mm} UTC._");
        return sb.ToString();
    }
}
