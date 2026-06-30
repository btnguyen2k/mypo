using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioAdd : BasePage
{
    [Parameter, SupplyParameterFromQuery(Name = "parentId")]
    public string? ParentId { get; set; }

    private string ParentPortfolioId { get; set; } = string.Empty;
    private string Name { get; set; } = string.Empty;
    private string Currency { get; set; } = string.Empty;
    private string Description { get; set; } = string.Empty;
    private string Viewers { get; set; } = string.Empty;
    private string DefaultMarketId { get; set; } = string.Empty;

    private IEnumerable<MarketDefResp> AllMarkets { get; set; } = [];

    private IEnumerable<PortfolioResp> MyPortfolioTree = [];

    private void DefaultMarketChanged()
    {
        var market = AllMarkets.FirstOrDefault(m => m.Id == DefaultMarketId);
        Currency = market?.Currency ?? string.Empty;
    }

    private void BtnClickCancel()
    {
        if (string.IsNullOrEmpty(ParentId))
        {
            NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO);
        }
        else
        {
            NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", ParentId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private async Task BtnClickSaveAndOpen()
    {
        await BtnClickSave(true);
    }

    private async Task BtnClickSave(bool openAfterCreate = false)
    {
        HideUI = true;
        ShowAlert("info", "Saving portfolio...");

        // Validate name
        if (string.IsNullOrWhiteSpace(Name))
        {
            HideUI = false;
            ShowAlert("warning", "Name is required.");
            return;
        }

        // Validate currency
        if (string.IsNullOrWhiteSpace(Currency))
        {
            HideUI = false;
            ShowAlert("warning", "Currency is required.");
            return;
        }

        var viewers = new HashSet<string>(Viewers?.ToLower().Split([',', ';', '\t', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);

        var req = new CreateOrUpdatePortfolioReq
        {
            Name = Name.Trim(),
            Description = Description.Trim(),
            Currency = Currency.ToUpper().Trim(),
            ParentId = ParentPortfolioId,
            Metadata = new()
            {
                Viewers = viewers,
                DefaultMarketId = DefaultMarketId,
            },
        };
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.CreatePortfolioAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            HideUI = false;
            ShowAlert("danger", resp.Message ?? "Error creating the portfolio.");
            return;
        }
        ShowAlert("success", "Portfolio created successfully. Navigating to my portfolio page...");
        var passAlertMessage = $"Portfolio '{req.Name}' created successfully.";
        var passAlertType = "success";
        await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}";
        if (openAfterCreate)
        {
            var pid = resp.Data?.Id ?? string.Empty;
            nextUrl = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", pid, StringComparison.OrdinalIgnoreCase);
            NavigationManager.NavigateTo($"{nextUrl}?alertMessage={passAlertMessage}&alertType={passAlertType}");
        }
        else
        {
            NavigationManager.NavigateTo(nextUrl);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            HideUI = true;
            var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

            ShowAlert("info", "Loading market info...");
            var marketResult = await apiClient.GetMarketsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
            if (marketResult.IsSuccess)
            {
                AllMarkets = marketResult.Data ?? [];
            }
            else
            {
                ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
                return;
            }

            ShowAlert("info", "Loading portfolio...");
            var portfolioResult = await apiClient.GetMyPortfoliosAsync(await GetAuthTokenAsync(), ApiBaseUrl);
            if (portfolioResult.IsSuccess)
            {
                var allPortfolios = portfolioResult.Data ?? [];
                MyPortfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);

                // preset parent when supplied via query (e.g. creating a child from a container portfolio)
                if (!string.IsNullOrEmpty(ParentId) && allPortfolios.Any(p => p.Id == ParentId))
                {
                    ParentPortfolioId = ParentId;
                }
            }
            else
            {
                ShowAlert("danger", portfolioResult.Message ?? "Error loading portfolio.");
                return;
            }

            HideUI = false;
            CloseAlert();
        }
    }
}
