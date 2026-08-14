using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioPlansDetails : BasePage
{
    [Parameter]
    public string PlanId { get; set; } = string.Empty;
    private PortfolioPlanResp SelectedPortfolioPlan { get; set; } = default!;
    private List<PortfolioPlanResp> PortfolioPlans { get; set; } = [];

    private CModal ModalDialogDelete { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await LoadPageAsync(PlanId);
        }
    }

    private async Task LoadPageAsync(string planId)
    {
        HideUI = true;
        ShowAlert("info", "Loading portfolio plan...");

        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();
        var planTask = apiClient.GetMyPortfolioPlanByIdAsync(planId, authToken, ApiBaseUrl);
        var plansTask = apiClient.GetMyPortfolioPlansAsync(authToken, ApiBaseUrl);
        var marketsTask = apiClient.GetMarketsAsync(authToken, ApiBaseUrl);
        await Task.WhenAll(planTask, plansTask, marketsTask);

        var planResp = await planTask;
        if (!planResp.IsSuccess || planResp.Data is null)
        {
            ShowAlert("danger", planResp.Message ?? "Error loading portfolio plan.");
            return;
        }

        var plansResp = await plansTask;
        if (!plansResp.IsSuccess)
        {
            ShowAlert("danger", plansResp.Message ?? "Error loading portfolio plans.");
            return;
        }

        var marketResult = await marketsTask;
        if (!marketResult.IsSuccess)
        {
            ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
            return;
        }

        SelectedPortfolioPlan = planResp.Data;
        PortfolioPlans = [.. plansResp.Data ?? []];
        SelectedPortfolioPlan.Market = marketResult.Data?.FirstOrDefault(m =>
            string.Equals(m.Id, SelectedPortfolioPlan.Portfolio?.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));
        ActiveAnalysisTab = TabIdSpotlight;

        HideUI = false;
        ShowPassedMessageOrCloseAlert();
    }

    private async Task BtnClickOpenPlan(string planId)
    {
        if (string.Equals(planId, SelectedPortfolioPlan.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_VIEW
            .Replace("{PlanId}", planId, StringComparison.OrdinalIgnoreCase);
        NavigationManager.NavigateTo(url);
        await LoadPageAsync(planId);
    }

    private void BtnClickEdit()
    {
        var id = SelectedPortfolioPlan.Id;
        NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_EDIT.Replace("{PlanId}", id, StringComparison.OrdinalIgnoreCase));
    }

    private void BtnClickDelete()
    {
        if (SelectedPortfolioPlan.OwnerUserId.Equals(CurrentUser?.Id, StringComparison.Ordinal))
        {
            ShowAlert("danger", "You are not authorized to delete this portfolio.");
            return;
        }
        ModalDialogDelete.Open();
    }

    private void BtnClickDeleteClose()
    {
        ModalDialogDelete.Close();
    }

    private async void BtnClickDeleteConfirm()
    {
        ModalDialogDelete.Close();
        HideUI = true;
        ShowAlert("info", $"Deleting portfolio plan '{SelectedPortfolioPlan.Name}', please wait...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var result = await apiClient.DeleteMyPortfolioPlanAsync(SelectedPortfolioPlan.Id, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!result.IsSuccess)
        {
            HideUI = false;
            ShowAlert("danger", result.Message ?? "Error deleting portfolio plan.");
            return;
        }

        ShowAlert("success", $"Portfolio plan '{SelectedPortfolioPlan.Name}' deleted successfully. Navigating to my portfolio plans page...");
        var passAlertMessage = $"Portfolio plan '{SelectedPortfolioPlan.Name}' deleted successfully.";
        var passAlertType = "success";
        await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS}?alertMessage={passAlertMessage}&alertType={passAlertType}";
        NavigationManager.NavigateTo(nextUrl);
    }

    public volatile bool analyzing = false;

    public async void BtnClickAnalyze()
    {
        if (analyzing) return;
        analyzing = true;
        var step = string.Empty;

        _ = Task.Run(async () =>
        {
            var start = DateTimeOffset.UtcNow;
            while (analyzing)
            {
                var delta = DateTimeOffset.UtcNow - start;
                if (analyzing)
                {
                    var stepStr = string.IsNullOrEmpty(step) ? string.Empty : $" - step: {step}";
                    ShowAlert("waiting", $"Analyzing portfolio plan '{SelectedPortfolioPlan.Name}'{stepStr}, please wait... ({delta.TotalSeconds}s)");
                }
                await Task.Delay(1000);
            }
        });
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        SelectedPortfolioPlan.Metadata ??= new();

        step = "spotlight";
        // Since FinHub v0.15.0+ no longer need to check for empty holdings as that is now handled at the server side.
        {
            var spotlightResult = await apiClient.SpotlightPortfolioPlanAsync(SelectedPortfolioPlan.Id, await GetAuthTokenAsync(), ApiBaseUrl);
            if (!spotlightResult.IsSuccess || spotlightResult.Data is null)
            {
                analyzing = false;
                ShowAlert("danger", spotlightResult.Message ?? "Error spotlighting portfolio plan.");
                return;
            }
            if (spotlightResult.Data.LLMError)
            {
                analyzing = false;
                ShowAlert("danger", $"Portfolio plan spotlight completed with LLM error: {spotlightResult.Data.LLMErrorMsg}");
                return;
            }
            SelectedPortfolioPlan.Metadata.SpotlightRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SelectedPortfolioPlan.Metadata.Spotlight = spotlightResult.Data.Analysis;
        }

        step = "analysis";
        {
            var analysisResult = await apiClient.AnalyzePortfolioPlanAsync(SelectedPortfolioPlan.Id, await GetAuthTokenAsync(), ApiBaseUrl);
            analyzing = false;
            if (!analysisResult.IsSuccess || analysisResult.Data is null)
            {
                ShowAlert("danger", analysisResult.Message ?? $"{analysisResult.Status}: Error analyzing portfolio plan.");
                return;
            }
            if (analysisResult.Data.LLMError)
            {
                ShowAlert("danger", $"Portfolio plan analysis completed with LLM error: {analysisResult.Data.LLMErrorMsg}");
                return;
            }
            SelectedPortfolioPlan.Metadata.AnalysisRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SelectedPortfolioPlan.Metadata.Analysis = analysisResult.Data.Analysis;
            SelectedPortfolioPlan.Metadata.RebalancePlan = analysisResult.Data.RebalancePlan;
        }

        ShowAlert("success", $"Portfolio plan '{SelectedPortfolioPlan.Name}' analyzed successfully.", autoCloseAfterMs: ALERT_AUTO_CLOSE_MS);
    }

    private const string TabIdSpotlight = "nav-spotlight-tab";
    private const string TabIdAnalysis = "nav-analysis-tab";
    private const string TabIdRebalancePlan = "nav-rebalance-plan-tab";
    private string ActiveAnalysisTab { get; set; } = TabIdSpotlight;

    private bool HasAnalysis => !string.IsNullOrEmpty(SelectedPortfolioPlan?.Metadata?.Analysis);
    private bool HasSpotlight => !string.IsNullOrEmpty(SelectedPortfolioPlan?.Metadata?.Spotlight);
    private bool HasRebalancePlan => !string.IsNullOrWhiteSpace(SelectedPortfolioPlan?.Metadata?.RebalancePlan);

    /// <summary>
    /// Resolves <see cref="ActiveAnalysisTab"/> to a tab that is actually visible (has content),
    /// preferring Spotlight, then Analysis, then Rebalance Plan.
    /// </summary>
    private string EffectiveAnalysisTab
    {
        get
        {
            if ((ActiveAnalysisTab == TabIdSpotlight && HasSpotlight)
                || (ActiveAnalysisTab == TabIdAnalysis && HasAnalysis)
                || (ActiveAnalysisTab == TabIdRebalancePlan && HasRebalancePlan))
            {
                return ActiveAnalysisTab;
            }
            if (HasSpotlight)
            {
                return TabIdSpotlight;
            }
            if (HasAnalysis)
            {
                return TabIdAnalysis;
            }
            if (HasRebalancePlan)
            {
                return TabIdRebalancePlan;
            }
            return ActiveAnalysisTab;
        }
    }

    private async void SwitchAnalysisTab(string tab)
    {
        ActiveAnalysisTab = tab;
        await InvokeAsync(StateHasChanged);
    }
}
