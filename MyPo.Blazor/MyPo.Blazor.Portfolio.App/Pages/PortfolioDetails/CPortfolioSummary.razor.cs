using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioSummary : CBase
{
    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<TxSettlementResp>? TxSettlements { get; set; }

    private PnlSummaryResp? PnlSummary { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private MarketDefResp? DefaultMarket => Markets?.FirstOrDefault(m => m.Id.Equals(Portfolio?.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));

    [Parameter]
    public IEnumerable<AssetResp>? Assets { get; set; }
    private decimal TotalMarketValue => Assets?
        .Where(a => a.Market?.Currency.Equals(Portfolio?.Currency, StringComparison.OrdinalIgnoreCase) ?? false)
        .Sum(a =>
        {
            if (MarketPricesMap.TryGetValue(a.Id, out var latestPrice))
            {
                return latestPrice * a.Quantity;
            }
            return 0;
        }) ?? 0;

    [Parameter]
    public Dictionary<string, StockQuote>? QuotesMap { get; set; } // map {asset-id --> quote}
    private Dictionary<string, decimal> MarketPricesMap { get; set; } = []; // map {asset-id --> market-price}
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && Portfolio is not null)
        {
            var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
            var pnlSummaryResp = await apiClient.GetMyPortfolioPnlSummaryAsync(Portfolio.Id, await GetAuthTokenAsync(), ApiBaseUrl);
            if (!pnlSummaryResp.IsSuccess)
            {
                ShowAlert("danger", pnlSummaryResp.Message ?? "Failed to load portfolio PnL summary.");
            }
            else
            {
                PnlSummary = pnlSummaryResp.Data;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Assets is not null && QuotesMap is not null)
        {
            MarketPricesMap.Clear();
            foreach (var asset in Assets)
            {
                var symbolKey = $"{asset.Market?.Code ?? string.Empty}:{asset.ItemCode}".ToUpper();
                if (QuotesMap.TryGetValue(symbolKey, out var quote))
                {
                    var latestPrice = quote.MarketPrice;
                    latestPrice /= (asset.Market?.PriceScale != 0 ? asset.Market?.PriceScale : 1) ?? 1;
                    MarketPricesMap[asset.Id] = latestPrice;
                }
            }
            await InvokeAsync(StateHasChanged);
        }
    }
}
