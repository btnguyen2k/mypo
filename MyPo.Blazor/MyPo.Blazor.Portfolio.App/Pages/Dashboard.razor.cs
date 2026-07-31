using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Dashboard : BasePage
{
    private static readonly TimeSpan StaleValuationAge = TimeSpan.FromHours(24);

    private List<PortfolioResp> Portfolios { get; set; } = [];

    private List<PortfolioResp> ActivePortfolios => Portfolios
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private List<PortfolioResp> InvestmentPortfolios => ActivePortfolios
        .Where(p => !(p.Metadata?.IsContainer ?? false))
        .ToList();

    private int ActiveContainerCount => ActivePortfolios.Count(p => p.Metadata?.IsContainer ?? false);

    private List<CurrencySummary> CurrencySummaries => InvestmentPortfolios
        .GroupBy(p => NormalizeCurrency(p.Currency), StringComparer.OrdinalIgnoreCase)
        .Select(group => new CurrencySummary
        {
            Currency = group.Key,
            PortfolioCount = group.Count(),
            TotalCosts = group.Sum(p => p.TotalCosts),
            MarketValue = group.Sum(p => p.TotalMarketValue),
        })
        .OrderBy(summary => summary.Currency, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private List<AttentionItem> AttentionItems => ActivePortfolios
        .Select(BuildAttentionItem)
        .Where(item => item is not null)
        .Cast<AttentionItem>()
        .OrderBy(item => item.Priority)
        .ThenBy(item => item.PortfolioName, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private List<PortfolioResp> PortfoliosWithReturns => InvestmentPortfolios
        .Where(p => p.TotalCosts > 0 && p.TotalMarketValue > 0)
        .ToList();

    private PortfolioResp? BestPerformer => PortfoliosWithReturns.MaxBy(p => p.TotalPnlPct);

    private PortfolioResp? WeakestPerformer => PortfoliosWithReturns.Count > 1
        ? PortfoliosWithReturns.MinBy(p => p.TotalPnlPct)
        : null;

    private string AggregatePnlTextClass
    {
        get
        {
            var summaries = CurrencySummaries;
            if (summaries.Any(summary => summary.TotalPnl > 0)
                && summaries.All(summary => summary.TotalPnl >= 0))
            {
                return "text-success";
            }
            if (summaries.Any(summary => summary.TotalPnl < 0)
                && summaries.All(summary => summary.TotalPnl <= 0))
            {
                return "text-danger";
            }
            return "text-muted";
        }
    }

    private string AggregatePnlBorderClass
    {
        get
        {
            return AggregatePnlTextClass switch
            {
                "text-success" => "border-start-success",
                "text-danger" => "border-start-danger",
                _ => "border-start-secondary",
            };
        }
    }

    private string BestPerformerBorderClass => BestPerformer?.TotalPnl switch
    {
        > 0 => "border-success",
        < 0 => "border-danger",
        _ => "border-secondary",
    };

    private string WeakestPerformerBorderClass => WeakestPerformer?.TotalPnl < 0
        ? "border-danger"
        : "border-secondary";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await LoadPortfoliosAsync();
        }
    }

    private async Task LoadPortfoliosAsync()
    {
        HideUI = true;
        ShowAlert("info", "Loading portfolio dashboard...");

        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var result = await apiClient.GetMyPortfoliosAsync(await GetAuthTokenAsync(), ApiBaseUrl);

        HideUI = false;
        if (!result.IsSuccess)
        {
            ShowAlert("danger", result.Message ?? "Error loading portfolios.");
            await InvokeAsync(StateHasChanged);
            return;
        }

        Portfolios = [.. result.Data ?? []];
        CloseAlert();
        await InvokeAsync(StateHasChanged);
    }

    private AttentionItem? BuildAttentionItem(PortfolioResp portfolio)
    {
        if (portfolio.Metadata is null)
        {
            return new AttentionItem(
                portfolio.Id,
                portfolio.Name,
                "Portfolio metadata is unavailable.",
                "bi-exclamation-triangle",
                "text-danger",
                0);
        }

        if (portfolio.Metadata.IsContainer)
        {
            var hasChildren = Portfolios.Any(p => string.Equals(p.ParentId, portfolio.Id, StringComparison.Ordinal));
            return hasChildren
                ? null
                : new AttentionItem(
                    portfolio.Id,
                    portfolio.Name,
                    "This container has no child portfolios.",
                    "bi-diagram-3",
                    "text-warning",
                    3);
        }

        if (portfolio.TotalCosts > 0 && portfolio.TotalMarketValue <= 0)
        {
            return new AttentionItem(
                portfolio.Id,
                portfolio.Name,
                "Current market value is unavailable.",
                "bi-exclamation-triangle",
                "text-danger",
                0);
        }

        if (portfolio.Metadata.MetadataRefreshTimestamp <= 0)
        {
            return new AttentionItem(
                portfolio.Id,
                portfolio.Name,
                "Valuation has not been refreshed.",
                "bi-clock-history",
                "text-warning",
                1);
        }

        var age = DateTimeOffset.UtcNow - portfolio.Metadata.MetadataRefreshUTC;
        if (age > StaleValuationAge)
        {
            return new AttentionItem(
                portfolio.Id,
                portfolio.Name,
                $"Valuation is stale ({FreshnessText(portfolio)}).",
                "bi-clock-history",
                "text-warning",
                2);
        }

        return null;
    }

    private string ParentName(PortfolioResp portfolio)
    {
        if (string.IsNullOrEmpty(portfolio.ParentId))
        {
            return string.Empty;
        }

        return Portfolios.FirstOrDefault(p => string.Equals(p.Id, portfolio.ParentId, StringComparison.Ordinal))?.Name
            ?? string.Empty;
    }

    private static string PortfolioDetailsUrl(string portfolioId)
    {
        return PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS
            .Replace("{PortfolioId}", portfolioId, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? "N/A"
            : currency.Trim().ToUpperInvariant();
    }

    private static string FormatAmount(decimal value)
    {
        return value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private static string FormatSignedAmount(decimal value)
    {
        return value switch
        {
            > 0 => $"+{FormatAmount(value)}",
            < 0 => FormatAmount(value),
            _ => "0.00",
        };
    }

    private static string FormatPercentage(decimal value)
    {
        return value switch
        {
            > 0 => $"+{value:P2}",
            _ => value.ToString("P2", CultureInfo.CurrentCulture),
        };
    }

    private static string PnlTextClass(decimal value)
    {
        return value switch
        {
            > 0 => "text-success",
            < 0 => "text-danger",
            _ => "text-muted",
        };
    }

    private static string FreshnessText(PortfolioResp portfolio)
    {
        var timestamp = portfolio.Metadata?.MetadataRefreshTimestamp ?? 0;
        if (timestamp <= 0)
        {
            return "Not refreshed";
        }

        var age = DateTimeOffset.UtcNow - portfolio.Metadata!.MetadataRefreshUTC;
        if (age < TimeSpan.Zero || age < TimeSpan.FromMinutes(1))
        {
            return "Just now";
        }
        if (age < TimeSpan.FromHours(1))
        {
            return $"{Math.Floor(age.TotalMinutes):N0} min ago";
        }
        if (age < TimeSpan.FromDays(1))
        {
            return $"{Math.Floor(age.TotalHours):N0} hr ago";
        }
        return $"{Math.Floor(age.TotalDays):N0} d ago";
    }

    private static string FreshnessBadgeClass(PortfolioResp portfolio)
    {
        var timestamp = portfolio.Metadata?.MetadataRefreshTimestamp ?? 0;
        if (timestamp <= 0)
        {
            return "text-bg-warning";
        }

        return DateTimeOffset.UtcNow - portfolio.Metadata!.MetadataRefreshUTC > StaleValuationAge
            ? "text-bg-warning"
            : "text-bg-light";
    }

    private sealed class CurrencySummary
    {
        public string Currency { get; init; } = string.Empty;
        public int PortfolioCount { get; init; }
        public decimal TotalCosts { get; init; }
        public decimal MarketValue { get; init; }
        public decimal TotalPnl => TotalCosts > 0 && MarketValue > 0 ? MarketValue - TotalCosts : 0;
        public decimal TotalPnlPct => TotalCosts > 0 ? TotalPnl / TotalCosts : 0;
    }

    private sealed record AttentionItem(
        string PortfolioId,
        string PortfolioName,
        string Message,
        string Icon,
        string TextClass,
        int Priority);
}
