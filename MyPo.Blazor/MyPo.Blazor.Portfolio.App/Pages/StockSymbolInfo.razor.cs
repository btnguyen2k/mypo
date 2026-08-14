using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Libs.Opurator;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public sealed partial class StockSymbolInfo : BasePage
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public string Symbol { get; set; } = string.Empty;

    private SymbolInfo? SymbolInfo { get; set; }

    private string PortfolioId { get; set; } = string.Empty;
    private AssetResp? OwningAsset { get; set; }

    private readonly List<MarketDefResp> Markets = [];
    private MarketDef? Market = null;

    private string Intent { get; set; } = string.Empty;

    private CModal ModalDialogAnalyzeSymbol { get; set; } = default!;
    private TickerAnalysis? TickerAnalysis { get; set; }

    private async void BtnClickAIAnalyze()
    {
        if (SymbolInfo is null)
        {
            ShowAlert("warning", "Empty symbol info.");
            return;
        }
        var alertMsg = $"Analyzing {Symbol} with AI...";
        TickerAnalysis = null;
        ModalDialogAnalyzeSymbol.Open();
        ModalDialogAnalyzeSymbol.ShowAlert("info", alertMsg);
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var req = new TickerAnalysisReq
        {
            Symbol = SymbolInfo.NormalizedSymbol,
            PortfolioId = PortfolioId,
            Intent = Intent,
        };
        var stopFlag = false;
        var startTimestamp = DateTime.UtcNow;
        _ = Task.Run(async () =>
        {
            while (!stopFlag)
            {
                var elapsed = DateTime.UtcNow - startTimestamp;
                if (elapsed > TimeSpan.FromSeconds(1))
                {
                    ModalDialogAnalyzeSymbol.ShowAlert("info", $"{alertMsg} {elapsed.TotalSeconds:F0} seconds.");
                }
                await Task.Delay(100);
            }
        });
        var analysisResponse = await apiClient.AnalyzeTickerAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
        stopFlag = true;
        if (!analysisResponse.IsSuccess)
        {
            ModalDialogAnalyzeSymbol.ShowAlert("danger", analysisResponse.Message ?? "Error analyzing symbol.");
            return;
        }
        TickerAnalysis = analysisResponse.Data;
        ModalDialogAnalyzeSymbol.CloseAlert();
    }

    private void BtnClickLoadData()
    {
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO.Replace("{Symbol}", Symbol, StringComparison.OrdinalIgnoreCase)}?{QUERY_PARM_REFRESH}=true";
        if (!string.IsNullOrEmpty(PortfolioId))
        {
            nextUrl += $"&pid={PortfolioId}";
        }
        NavigationManager.NavigateTo(nextUrl);
    }

    private void BtnClickReturnToPortfolio()
    {
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}";
        NavigationManager.NavigateTo(nextUrl);
    }

    private int RefreshBackgroundTaskId = Random.Shared.Next();

    private void StopRefresBackground()
    {
        RefreshBackgroundTaskId = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopRefresBackground();
        }
        base.Dispose(disposing);
    }

    private async Task<ApiResp<SymbolInfo>> FetchSymbolInfo(string symbol)
    {
        SetBackgroundMsg($"⌛Loading symbol info for {symbol}...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var symbolResult = await apiClient.GetStockSymbolInfoAsync(Symbol, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!symbolResult.IsSuccess)
        {
            SetBackgroundMsg($"❗Error loading symbol info for {symbol}. Status: {symbolResult.Status}, Message: {symbolResult.Message}");
        }
        return symbolResult;
    }

    private async void InitializePage()
    {
        HideUI = true;

        ShowAlert("info", "Loading symbol info...");
        SymbolInfo = null;
        var symbolResult = await FetchSymbolInfo(Symbol);
        if (!symbolResult.IsSuccess || symbolResult.Data is null)
        {
            ShowAlert("danger", symbolResult.Message ?? $"Error loading symbol info for {Symbol}.");
            return;
        }
        SymbolInfo = symbolResult.Data;

        var parts = SymbolInfo.NormalizedSymbol.Split(":") ?? [];
        var (exchange, symbol) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
        Market = Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase))?.ToModel();

        if (!string.IsNullOrEmpty(PortfolioId))
        {
            ShowAlert("info", "Loading owning asset info...");
            OwningAsset = null;
            var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
            var apiResult = await apiClient.GetMyPortfolioAssetsAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
            if (!apiResult.IsSuccess)
            {
                ShowAlert("danger", apiResult.Message ?? $"Error loading owning asset info for {Symbol}.");
                return;
            }
            OwningAsset = apiResult.Data?.FirstOrDefault(a =>
                string.Equals(a.ItemCode, symbol, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.Market?.Code, exchange, StringComparison.OrdinalIgnoreCase)
            );
        }

        var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
        taskOperator.ExecuteInBackground(() => LoadSymbolInfoBackground(Symbol, RefreshBackgroundTaskId = Random.Shared.Next()));

        HideUI = false;
        CloseAlert();
    }

    private async void LoadSymbolInfoBackground(string symbol, int myTaskId)
    {
        if (myTaskId == RefreshBackgroundTaskId)
        {
            if (Market == null)
            {
                SetBackgroundMsg($"❗Market info not found. Cannot determine refresh timing for symbol info.");
                return;
            }
            var sleepTime = Random.Shared.NextInt64(10 * 1000, 20 * 1000);
            if (!Market.IsCurrentlyOpen())
            {
                var timeTillOpen = Market.TimeTillOpen();
                if (timeTillOpen > TimeSpan.FromMinutes(60))
                {
                    SetBackgroundMsg($"❗Market '{Market.Code}' is currently closed. Not refreshing.");
                    return;
                }
                sleepTime = Random.Shared.NextInt64(5 * 60 * 1000, 10 * 60 * 1000);
                sleepTime = Math.Min(sleepTime, (long)timeTillOpen.TotalMilliseconds) + 1000;
            }
            while (sleepTime > 0 && myTaskId == RefreshBackgroundTaskId)
            {
                SetBackgroundMsg($"💤Sleeping {sleepTime / 1000} seconds before next info refresh...");
                var delay = Math.Min(sleepTime, 1000);
                await Task.Delay((int)delay);
                sleepTime -= delay;
            }
            if (myTaskId == RefreshBackgroundTaskId)
            {
                var symbolResult = await FetchSymbolInfo(symbol);
                if (symbolResult.Status == 200)
                {
                    SymbolInfo = symbolResult.Data;
                    await InvokeAsync(StateHasChanged);
                }
                await Task.Run(() => LoadSymbolInfoBackground(symbol, myTaskId));
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // store the portfolio id for later.
        var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
        PortfolioId = queryParams.Get("pid") ?? string.Empty;
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
            if (marketResult.Status != 200)
            {
                ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
                return;
            }
            Markets.AddRange(marketResult.Data ?? []);

            CloseAlert();

            HideUI = false;
        }

        if (firstRender && !string.IsNullOrEmpty(Symbol))
        {
            InitializePage();
        }

        if (RemoveRefreshParamIfPresent()) await Task.Run(InitializePage);
    }
}
