using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Dashboard : BasePage
{
    private static readonly TimeSpan StaleValuationAge = TimeSpan.FromHours(24);
    private const int MinActionableRebalancePlanLength = 100;
    private static readonly Regex SpotlightRiskSummaryRegex = MyRegexSpotlightRiskSummary();

    private List<PortfolioResp> Portfolios { get; set; } = [];
    private List<PortfolioPlanResp> PortfolioPlans { get; set; } = [];

    private List<PortfolioResp> ActivePortfolios => [..Portfolios
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
    ];

    private List<PortfolioResp> InvestmentPortfolios => [..ActivePortfolios
        .Where(p => !(p.Metadata?.IsContainer ?? false) && (GetMarketValue(p) > 0m || (p.Metadata?.TotalBuys ?? 0m) > 0m))
    ];

    private List<CurrencySummary> CurrencySummaries => [..InvestmentPortfolios
        .GroupBy(p => NormalizeCurrency(p.Currency), StringComparer.OrdinalIgnoreCase)
        .Select(group => new CurrencySummary
        {
            Currency = group.Key,
            CostBasic = group.Sum(p => p.Metadata?.CostBasic ?? 0m),
            MarketValue = group.Sum(p => p.Metadata?.MarketValue ?? 0m),
            TotalInvestment = group.Sum(p => p.Metadata?.TotalInvestment ?? 0m),
            TotalReturn = group.Sum(p => p.Metadata?.TotalReturn ?? 0m),
        })
        .OrderBy(summary => summary.Currency, StringComparer.OrdinalIgnoreCase)
    ];

    private List<AttentionItem> AttentionItems
    {
        get
        {
            return [.. PortfolioPlans
                .SelectMany(BuildPlanAttentionItems)
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)];
        }
    }

    private List<PortfolioResp> PortfoliosWithReturns => [..InvestmentPortfolios
        .Where(p => (p.Metadata?.TotalInvestment??0) > 0 && (p.Metadata?.TotalReturn??0) > 0)
    ];

    private PortfolioResp? BestPerformer => PortfoliosWithReturns.MaxBy(p => p.Metadata?.TotalUnrealizedPnlPct??0m);

    private PortfolioResp? WeakestPerformer => PortfoliosWithReturns.Count > 1
        ? PortfoliosWithReturns.MinBy(p => p.Metadata?.TotalUnrealizedPnlPct??0m)
        : null;

    private string AggregatePnlTextClass => GetAggregatePnlTextClass(summary => summary.UnrealizedPnl);

    private string AggregatePnlBorderClass => GetAggregatePnlBorderClass(AggregatePnlTextClass);

    private string AggregateTotalUnrealizedPnlTextClass =>
        GetAggregatePnlTextClass(summary => summary.TotalUnrealizedPnl);

    private string AggregateTotalUnrealizedPnlBorderClass =>
        GetAggregatePnlBorderClass(AggregateTotalUnrealizedPnlTextClass);

    private string GetAggregatePnlTextClass(Func<CurrencySummary, decimal> valueSelector)
    {
        var values = CurrencySummaries.Select(valueSelector).ToList();
        if (values.Any(value => value > 0) && values.All(value => value >= 0))
        {
            return "text-success";
        }
        if (values.Any(value => value < 0) && values.All(value => value <= 0))
        {
            return "text-danger";
        }
        return "text-muted";
    }

    private static string GetAggregatePnlBorderClass(string textClass)
    {
        return textClass switch
        {
            "text-success" => "border-start-success",
            "text-danger" => "border-start-danger",
            _ => "border-start-secondary",
        };
    }

    private string BestPerformerBorderClass => BestPerformer is null
        ? "border-secondary"
        : GetUnrealizedPnl(BestPerformer) switch
    {
        > 0 => "border-success",
        < 0 => "border-danger",
        _ => "border-secondary",
    };

    private string WeakestPerformerBorderClass => WeakestPerformer is not null && GetUnrealizedPnl(WeakestPerformer) < 0
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
        var authToken = await GetAuthTokenAsync();
        var portfolioTask = apiClient.GetMyPortfoliosAsync(authToken, ApiBaseUrl);
        var portfolioPlanTask = apiClient.GetMyPortfolioPlansAsync(authToken, ApiBaseUrl);
        await Task.WhenAll(portfolioTask, portfolioPlanTask);

        HideUI = false;
        var portfolioResult = await portfolioTask;
        if (!portfolioResult.IsSuccess)
        {
            Portfolios = [];
            ShowAlert("danger", portfolioResult.Message ?? "Error loading portfolios.");
            return;
        }
        Portfolios = [.. portfolioResult.Data ?? []];

        var portfolioPlanResult = await portfolioPlanTask;
        if (!portfolioPlanResult.IsSuccess)
        {
            PortfolioPlans = [];
            ShowAlert("warning", portfolioPlanResult.Message ?? "Error loading portfolio plans.");
            return;
        }
        PortfolioPlans = [.. portfolioPlanResult.Data ?? []];

        CloseAlert();
    }

    private static IEnumerable<AttentionItem> BuildPlanAttentionItems(PortfolioPlanResp plan)
    {
        var riskCount = GetSpotlightRiskCount(plan.Metadata?.Spotlight);
        if (riskCount > 0)
        {
            yield return new AttentionItem(
                PortfolioPlanDetailsUrl(plan.Id),
                plan.Name,
                $"Spotlight identified {riskCount} critical/high {(riskCount == 1 ? "risk" : "risks")}.",
                "bi-shield-exclamation",
                "text-danger",
                "Risk",
                "text-bg-danger",
                0);
        }

        if (HasActionableRebalancePlan(plan.Metadata?.RebalancePlan))
        {
            yield return new AttentionItem(
                PortfolioPlanDetailsUrl(plan.Id),
                plan.Name,
                "A rebalance plan is available.",
                "bi-arrow-left-right",
                "text-warning",
                "Rebalance",
                "text-bg-warning",
                1);
        }
    }

    private static int GetSpotlightRiskCount(string? spotlight)
    {
        if (string.IsNullOrWhiteSpace(spotlight))
        {
            return 0;
        }

        var match = SpotlightRiskSummaryRegex.Match(spotlight);
        return match.Success
            && int.TryParse(match.Groups["count"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            && count > 0
                ? count
                : 0;
    }

    private static bool HasActionableRebalancePlan(string? rebalancePlan)
    {
        if (string.IsNullOrWhiteSpace(rebalancePlan))
        {
            return false;
        }

        var normalizedPlan = rebalancePlan.Trim();
        return normalizedPlan.Length >= MinActionableRebalancePlanLength
            && !normalizedPlan.Contains("No rebalance needed", StringComparison.OrdinalIgnoreCase);
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

    private static string PortfolioPlanDetailsUrl(string portfolioPlanId)
    {
        return PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_VIEW
            .Replace("{PlanId}", portfolioPlanId, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? "N/A"
            : currency.Trim().ToUpperInvariant();
    }

    private static decimal GetCostBasic(PortfolioResp portfolio)
    {
        return portfolio.Metadata?.CostBasic ?? 0m;
    }

    private static decimal GetMarketValue(PortfolioResp portfolio)
    {
        return portfolio.Metadata?.MarketValue ?? 0m;
    }

    private static decimal GetUnrealizedPnl(PortfolioResp portfolio)
    {
        return portfolio.Metadata?.UnrealizedPnl ?? 0m;
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
        public decimal CostBasic { get; init; }
        public decimal MarketValue { get; init; }
        public decimal UnrealizedPnl => CostBasic > 0 && MarketValue > 0 ? MarketValue - CostBasic : 0;
        public decimal UnrealizedPnlPct => CostBasic > 0 ? UnrealizedPnl / CostBasic : 0;
        public decimal TotalInvestment { get; init; }
        public decimal TotalReturn { get; init; }
        public decimal TotalUnrealizedPnl => TotalReturn - TotalInvestment;
        public decimal TotalUnrealizedPnlPct => TotalInvestment > 0 ? TotalUnrealizedPnl / TotalInvestment : 0;
    }

    private sealed record AttentionItem(
        string TargetUrl,
        string Title,
        string Message,
        string Icon,
        string TextClass,
        string BadgeText,
        string BadgeClass,
        int Priority);

    [GeneratedRegex(@"SUMMARY:\s*(?<count>\d+)\s+Critical/High risks with actions[.!]?(?:\s|[*_])*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegexSpotlightRiskSummary();
}
