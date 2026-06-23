using Microsoft.AspNetCore.Components;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioPreferences : CBase
{
    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private MarketDefResp? DefaultMarket => Markets?.FirstOrDefault(m => m.Id.Equals(Portfolio?.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && Portfolio is not null)
        {
            // TODO
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // TODO
    }
}
