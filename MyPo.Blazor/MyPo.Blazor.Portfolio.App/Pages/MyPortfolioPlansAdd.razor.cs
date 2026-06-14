using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioPlansAdd : BasePage
{
    private string Type { get; set; } = PortfolioPlanEntity.PLAN_TYPE_ALLOCATION;
    private string Name { get; set; } = string.Empty;
    private string Description { get; set; } = string.Empty;
    private string PortfolioId { get; set; } = string.Empty;
    private IEnumerable<PortfolioResp> MyPortfolioTree { get; set; } = [];
    private List<HoldingTicker> HoldingTickers { get; set; } = [new HoldingTicker()];
    private decimal TotalHoldingsPercent => HoldingTickers.Sum(ht => ht.TargetAllocation / 100);

    private string InvalidLine = string.Empty;
    private string InvalidComponent = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            HideUI = true;
            var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

            ShowAlert("info", "Loading portfolios...");
            var portfolioResult = await apiClient.GetMyPortfoliosAsync(await GetAuthTokenAsync(), ApiBaseUrl);
            if (!portfolioResult.IsSuccess)
            {
                ShowAlert("danger", portfolioResult.Message ?? "Error loading portfolios.");
                return;
            }

            var allPortfolios = portfolioResult.Data ?? [];
            MyPortfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
            HideUI = false;
            CloseAlert();
        }
    }

    private void BtnClickAddTicker()
    {
        HoldingTickers.Add(new HoldingTicker());
        StateHasChanged();
    }

    private void BtnClickRemoveTicker(string id)
    {
        var tickerToRemove = HoldingTickers.FirstOrDefault(ht => ht.Id == id);
        if (tickerToRemove != null)
        {
            HoldingTickers.Remove(tickerToRemove);
            StateHasChanged();
        }
    }

    private void BtnClickCancel()
    {
        NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS);
    }

    private async Task BtnClickSaveAndOpen()
    {
        await BtnClickSave(true);
    }

    private async Task BtnClickSave(bool openAfterSave = false)
    {
        HideUI = true;
        ShowAlert("info", "Saving portfolio plan...");

        // Validate name
        if (string.IsNullOrWhiteSpace(Name))
        {
            HideUI = false;
            ShowAlert("warning", "Name is required.");
            return;
        }

        // validate tickers and allocation
        foreach (var ticker in HoldingTickers)
        {
            if (string.IsNullOrWhiteSpace(ticker.Ticker))
            {
                HideUI = false;
                (InvalidLine, InvalidComponent) = (ticker.Id, "Ticker");
                ShowAlert("warning", "Ticker is required.");
                return;
            }
            if (ticker.TargetAllocation < 0)
            {
                HideUI = false;
                (InvalidLine, InvalidComponent) = (ticker.Id, "Allocation");
                ShowAlert("warning", "Target allocation must be >= 0.");
                return;
            }
        }
        // if (TotalHoldingsPercent != 1.0m)
        // {
        // 	HideUI = false;
        // 	ShowAlert("warning", "Total allocation must be 100%.");
        // 	return;
        // }

        var req = new CreateOrUpdatePortfolioPlanReq
        {
            Type = Type,
            PortfolioId = PortfolioId,
            Name = Name.Trim(),
            Metadata = new()
            {
                HoldingTickers = HoldingTickers,
                Description = Description.Trim(),
            },
        };
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.CreatePortfolioPlanAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            HideUI = false;
            ShowAlert("danger", resp.Message ?? "Error creating the portfolio plan.");
            return;
        }

        ShowAlert("success", "Portfolio plan created successfully. Navigating to my portfolio plans page...");
        var passAlertMessage = $"Portfolio plan '{req.Name}' created successfully.";
        var passAlertType = "success";
        await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS}?alertMessage={passAlertMessage}&alertType={passAlertType}";
        if (openAfterSave)
        {
            var pid = resp.Data?.Id ?? string.Empty;
            nextUrl = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_VIEW.Replace("{PlanId}", pid, StringComparison.OrdinalIgnoreCase);
            NavigationManager.NavigateTo($"{nextUrl}?alertMessage={passAlertMessage}&alertType={passAlertType}");
        }
        else
        {
            NavigationManager.NavigateTo(nextUrl);
        }
    }
}
