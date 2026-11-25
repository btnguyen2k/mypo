using FinHub.Client.Models.Stocks;
using Microsoft.AspNetCore.Components;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CStockChart : BaseComponent
{
    [Parameter]
    public SymbolInfo? SymbolInfo { get; set; }

    [Parameter]
    public MarketDef? Market { get; set; }

    private string? ErrorMessage { get; set; }

    private readonly HashSet<string> ChartsUseVietstock = ["*VNVN", "HOSE", "HNX", "UPCOM"];

    private bool UseVietstockChart => ChartsUseVietstock.Contains(SymbolInfo?.Exchange ?? "");
    private bool UseTradingViewChart => !UseVietstockChart;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            ErrorMessage = null;
            if (SymbolInfo is null)
            {
                ErrorMessage = "No Chart Available.";
            }
            else if (!UseVietstockChart && !UseTradingViewChart)
            {
                ErrorMessage = $"Charting for exchange '{SymbolInfo.Exchange}' is not supported.";
            }
            await InvokeAsync(StateHasChanged);
        }
    }
}
