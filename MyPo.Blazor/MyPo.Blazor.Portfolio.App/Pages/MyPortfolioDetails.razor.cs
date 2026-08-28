using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using Microsoft.JSInterop;
using MyPo.Libs.Opurator;
using MyPo.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Blazor.Portfolio.App.Shared;
using FinHub.Client.Models.Stocks;
using FinHub.Client.Schemas.Stocks;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioDetails : BasePage
{
    [Parameter]
    public string PortfolioId { get; set; } = string.Empty;
    private PortfolioResp? SelectedPortfolio { get; set; }

    private bool IsPortfolioOwner() => SelectedPortfolio?.OwnerUserId.Equals(CurrentUser?.Id, StringComparison.OrdinalIgnoreCase) ?? false;

    private Dictionary<string, PortfolioResp> PortfoliosMap { get; set; } = [];

    // root portfolios (with Children populated) used to render the breadcrumb jump-to dropdown as a tree
    private IEnumerable<PortfolioResp> PortfolioRoots { get; set; } = [];

    // depth-first flatten of the portfolio tree into (portfolio, indent-level) pairs, ordered by name
    private IEnumerable<(PortfolioResp Portfolio, int Level)> FlattenPortfolioTree()
    {
        static IEnumerable<(PortfolioResp, int)> Walk(PortfolioResp p, int level)
        {
            yield return (p, level);
            foreach (var child in p.Children ?? [])
            {
                foreach (var descendant in Walk(child, level + 1))
                {
                    yield return descendant;
                }
            }
        }
        foreach (var root in PortfolioRoots)
        {
            foreach (var node in Walk(root, 0))
            {
                yield return node;
            }
        }
    }

    private IEnumerable<MarketDefResp>? Markets { get; set; }
    private IEnumerable<TxBuySellResp>? TxBuySells { get; set; }
    private IEnumerable<AssetResp>? Assets { get; set; }
    private IEnumerable<TxSettlementResp>? TxSettlements { get; set; }

    // map {asset-id --> quote}
    private readonly Dictionary<string, StockQuote> QuotesMap = [];

    private int RefreshBackgroundTaskId = Random.Shared.Next();

    private void StopRefreshQuotesBackground()
    {
        RefreshBackgroundTaskId = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopRefreshQuotesBackground();
        }
        base.Dispose(disposing);
    }

    private async void GetStocksQuotesBackground(List<string> symbolsList, int myTaskId)
    {
        var hasError = false;

        void prefetchCallback(IEnumerable<string> symbols)
        {
            SetBackgroundMsg($"⌛Fetching quotes for symbols: {string.Join(",", symbols)}");
        }

        bool postfetchCallback(GetStockQuotesResponse quotesResp)
        {
            var emptyDict = new Dictionary<string, StockQuote>();
            if (!quotesResp.IsSuccess)
            {
                hasError = true;
                SetBackgroundMsg($"❗Failed to fetch quotes for symbols: {string.Join(",", symbolsList)}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
            }
            else
            {
                foreach (var quote in quotesResp.Data ?? emptyDict)
                {
                    QuotesMap[quote.Key] = quote.Value;
                }
                InvokeAsync(StateHasChanged);
            }
            return myTaskId == RefreshBackgroundTaskId;
        }

        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();
        if (symbolsList.Count > 0 && myTaskId == RefreshBackgroundTaskId)
        {
            await TickerUtils.FetchQuotesForTickers(symbolsList, apiClient, authToken, ApiBaseUrl, prefetchCallback, postfetchCallback);
        }

        var metadataUpdateDelay = TimeSpan.FromMinutes(60);
        if (!hasError && myTaskId == RefreshBackgroundTaskId && IsPortfolioOwner() &&
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() - metadataUpdateDelay.TotalSeconds > (SelectedPortfolio!.Metadata?.MetadataRefreshTimestamp ?? 0))
        {
            // Sync portfolio metadata
            var req = CreateOrUpdatePortfolioReq.NewRequest(SelectedPortfolio!);

            req.Metadata!.CostBasic = Assets?
                .Where(a => a.Market?.Currency.Equals(SelectedPortfolio?.Currency, StringComparison.OrdinalIgnoreCase) ?? false)
                .Sum(a => a.AveragePrice * a.Quantity) ?? 0;
            req.Metadata!.MarketValue = Assets?
                .Where(a => a.Market?.Currency.Equals(SelectedPortfolio?.Currency, StringComparison.OrdinalIgnoreCase) ?? false)
                .Sum(a =>
                {
                    var symbol = $"{a.Market?.Code ?? string.Empty}:{a.ItemCode}";
                    return (QuotesMap.TryGetValue(symbol, out var quote) ? quote.MarketPrice : 0) * a.Quantity;
                }) ?? 0;
            req.Metadata!.TotalBuys = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_BUY)
                .Sum(t => t.TxValue) ?? 0;
            req.Metadata!.TotalSells = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_SELL)
                .Sum(t => t.TxValue) ?? 0;
            req.Metadata!.TotalFees = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_FEE)
                .Sum(t => t.TxValue) ?? 0;
            req.Metadata!.TotalTax = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_TAX)
                .Sum(t => t.TxValue) ?? 0;
            req.Metadata!.TotalInterest = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_INTEREST)
                .Sum(t => t.TxValue) ?? 0;
            req.Metadata!.TotalIncome = TxSettlements?
                .Where(t => t.TxType == TxSettlementEntity.TX_TYPE_DISTRIBUTION || t.TxType == TxSettlementEntity.TX_TYPE_DIVIDEND)
                .Sum(t => t.TxValue) ?? 0;
            var market = Markets?.FirstOrDefault(m => string.Equals(m.Id, SelectedPortfolio!.Metadata!.DefaultMarketId, StringComparison.OrdinalIgnoreCase))?.ToModel();
            if (market is not null && "VND".Equals(market.Currency, StringComparison.OrdinalIgnoreCase))
            {
                // special case for VN market
                req.Metadata!.MarketValue /= 1000;
            }
            req.Metadata!.MetadataRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var resp = await apiClient.UpdateMyPortfolioAsync(SelectedPortfolio!.Id, req, authToken, ApiBaseUrl);
            if (resp.IsSuccess)
            {
                SelectedPortfolio = resp.Data;
                await InvokeAsync(StateHasChanged);
            }

            if (myTaskId == RefreshBackgroundTaskId)
            {
                var sleepTime = Random.Shared.NextInt64(30 * 1000, 60 * 1000);
                if (market == null)
                {
                    SetBackgroundMsg($"❗Default market '{SelectedPortfolio?.Metadata?.DefaultMarketId}' not found in markets metadata. Not refreshing quotes.");
                    return;
                }
                if (!market.IsCurrentlyOpen())
                {
                    var timeTillOpen = market.TimeTillOpen();
                    if (timeTillOpen > TimeSpan.FromMinutes(60))
                    {
                        SetBackgroundMsg($"❗Market '{market.Id}' is currently closed. Not refreshing.");
                        return;
                    }
                    sleepTime = Random.Shared.NextInt64(5 * 60 * 1000, 10 * 60 * 1000);
                    sleepTime = Math.Min(sleepTime, (long)timeTillOpen.TotalMilliseconds) + 1000;
                }
                while (sleepTime > 0 && myTaskId == RefreshBackgroundTaskId)
                {
                    SetBackgroundMsg($"💤Sleeping {sleepTime / 1000} seconds before next quotes refresh...");
                    var delay = Math.Min(sleepTime, 1000);
                    await Task.Delay((int)delay);
                    sleepTime -= delay;
                }
                if (myTaskId == RefreshBackgroundTaskId)
                {
                    await Task.Run(() => GetStocksQuotesBackground(symbolsList, myTaskId));
                }
            }
        }
    }

    private async Task<PortfolioResp?> LoadPortfolioAsync(string id, string authToken)
    {
        HideUI = true;
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

        ShowAlert("info", "Loading portfolio, please wait...");
        var apiRespPortfolio = await apiClient.GetMyPortfoliosAsync(authToken, ApiBaseUrl);
        if (!apiRespPortfolio.IsSuccess)
        {
            ShowAlert("danger", apiRespPortfolio.Message ?? "Error while loading portfolio.");
            return null;
        }

        PortfoliosMap = (apiRespPortfolio.Data ?? []).ToDictionary(p => p.Id);
        if (!PortfoliosMap.TryGetValue(id, out var portfolio))
        {
            ShowAlert("danger", $"Portfolio '{id}' not found.");
            return null;
        }

        // build the tree so container portfolios have their child portfolios populated
        PortfolioRoots = PortfolioUtils.BuildPortfolioTree(apiRespPortfolio.Data ?? []);

        if (portfolio.Metadata?.IsContainer ?? false)
        {
            // container portfolios have no assets/transactions of their own: skip loading them
            return portfolio;
        }

        ShowAlert("info", "Loading portfolio buy/sell transactions, please wait...");
        var apiRespTx = await apiClient.GetMyPortfolioTxBuySellsAsync(portfolio.Id, authToken, ApiBaseUrl);
        if (!apiRespTx.IsSuccess)
        {
            ShowAlert("danger", apiRespTx.Message ?? $"Error while loading portfolio buy/sell transactions.");
            return null;
        }
        TxBuySells = apiRespTx.Data ?? [];

        ShowAlert("info", "Loading portfolio assets, please wait...");
        var apiRespAssets = await apiClient.GetMyPortfolioAssetsAsync(portfolio.Id, authToken, ApiBaseUrl);
        if (!apiRespAssets.IsSuccess)
        {
            ShowAlert("danger", apiRespAssets.Message ?? $"Error while loading portfolio assets.");
            return null;
        }
        Assets = apiRespAssets.Data ?? [];

        ShowAlert("info", "Loading portfolio settlement records, please wait...");
        var apiRespTxSettlementRecs = await apiClient.GetMyPortfolioTxSettlementsAsync(portfolio.Id, authToken, ApiBaseUrl);
        if (!apiRespTxSettlementRecs.IsSuccess)
        {
            ShowAlert("danger", apiRespTxSettlementRecs.Message ?? $"Error while loading portfolio settlement records.");
            return null;
        }
        TxSettlements = apiRespTxSettlementRecs.Data ?? [];

        return portfolio;
    }

    private async Task<IEnumerable<MarketDefResp>?> LoadMarketsAsync(string authToken)
    {
        ShowAlert("info", "Loading markets metadata...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var result = await apiClient.GetMarketsAsync(authToken, ApiBaseUrl);
        if (result.IsSuccess)
        {
            return result.Data ?? [];
        }
        ShowAlert("danger", result.Message ?? "Unknown error while loading markets metadata.");
        return null;
    }

    private async void AutoPopulateAssetMetadata()
    {
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var authToken = await GetAuthTokenAsync();
        var assetsToUpdate = (Assets ?? [])
            .Where(a => a.Metadata is null
                || string.IsNullOrEmpty(a.Metadata.CorpName)  // corp name is mandatory
                || string.IsNullOrEmpty(a.Metadata.AssetType) // asset type is mandatory
                || a.Metadata.Tags is null || a.Metadata.Tags.Count == 0 // tags should not be empty
                || (a.Metadata.AssetType != "ETF" && (string.IsNullOrEmpty(a.Metadata.Sector) || string.IsNullOrEmpty(a.Metadata.Industry))) // not ETF but sector/industry is empty
            ).ToList();
        foreach (var a in assetsToUpdate)
        {
            var symbol = $"{a.Market?.Code}:{a.ItemCode}";
            SetBackgroundMsg($"🔍Fetching overview info for asset '{symbol}'...");
            var apiResp = await apiClient.GetStockSymbolOverviewAsync(symbol, authToken, ApiBaseUrl);
            if (!apiResp.IsSuccess)
            {
                SetBackgroundMsg($"❗Failed to fetch overview info for asset '{symbol}'. Status: {apiResp.Status}, Message: {apiResp.Message}");
            }
            else
            {
                var overview = apiResp.Data;
                if (overview is null) continue;

                a.Metadata ??= new AssetMetadata();
                a.Metadata.CorpName = overview.LongName?.Trim() ?? overview.ShortName?.Trim() ?? string.Empty;
                a.Metadata.Industry = overview.Industry?.Trim() ?? string.Empty;
                a.Metadata.Sector = overview.Sector?.Trim() ?? string.Empty;
                a.Metadata.AssetType = overview.AssetType?.ToString().Trim().ToUpper() ?? "N/A";
                a.Metadata.Tags = new HashSet<string>(a.Metadata.Tags ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(a.Metadata.Industry))
                {
                    a.Metadata.Tags.Add(a.Metadata.Industry);
                }
                if (a.Metadata.AssetType == "ETF")
                {
                    a.Metadata.Tags.Add("ETF");
                }
                if (!IsPortfolioOwner()) continue;
                SetBackgroundMsg($"⌛Updating asset metadata for '{symbol}'...");
                var updateReq = CreateOrUpdateAssetReq.NewRequest(a);
                var updateResp = await apiClient.UpdateMyPortfolioAssetAsync(updateReq, await GetAuthTokenAsync(), ApiBaseUrl);
                if (!updateResp.IsSuccess)
                {
                    SetBackgroundMsg($"❗Failed to update asset metadata for '{symbol}'. Status: {updateResp.Status}, Message: {updateResp.Message}");
                }
                else
                {
                    SetBackgroundMsg($"✅Successfully updated asset metadata for '{symbol}'.");
                }
            }
        }
    }

    private async void InitializePage()
    {
        HideUI = true;

        SwitchToSavedTab();

        Markets = await LoadMarketsAsync(await GetAuthTokenAsync());

        SelectedPortfolio = await LoadPortfolioAsync(PortfolioId, await GetAuthTokenAsync());
        if (SelectedPortfolio == null) return;

        if (!(SelectedPortfolio.Metadata?.IsContainer ?? false))
        {
            var symbolsList = Assets?
                .Where(a => a.Quantity > 0) // optimization: only fetch quotes for assets positive holdings
                .Select(a =>
                {
                    var market = Markets?.FirstOrDefault(m => string.Equals(m.Id, a.MarketId, StringComparison.OrdinalIgnoreCase));
                    return $"{market?.Code ?? string.Empty}:{a.ItemCode}";
                })
                .Distinct()
                .ToList() ?? [];
            SetBackgroundMsg($"ℹ️Initializing page for portfolio '{SelectedPortfolio.Name}' with {Assets?.Count() ?? 0} assets. Symbols: {string.Join(", ", symbolsList)}");
            var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
            taskOperator.ExecuteInBackground(() => GetStocksQuotesBackground(symbolsList, RefreshBackgroundTaskId = Random.Shared.Next()));
            taskOperator.ExecuteInBackground(() => AutoPopulateAssetMetadata());
        }

        HideUI = false;

        ShowPassedMessageOrCloseAlert();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender) InitializePage();

        if (RemoveRefreshParamIfPresent()) await Task.Run(InitializePage);
    }

    private string ActiveTab { get; set; } = TabIdSummary;
    private const string TabIdSummary = "nav-summary-tab";
    private const string TabIdPositions = "nav-positions-tab";
    private const string TabIdTxBuysSells = "nav-txbuyssells-tab";
    private const string TabIdTxSettled = "nav-txsettled-tab";
    private const string TabIdSnapshots = "nav-snapshots-tab";
    private const string TabIdTrends = "nav-trends-tab";
    private const string TabIdPreferences = "nav-preferences-tab";

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private void BtnClickOpenPortfolio(string pid)
    {
        if (string.Equals(pid, PortfolioId, StringComparison.OrdinalIgnoreCase))
        {
            // already viewing this portfolio: nothing to do
            return;
        }
        NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", pid, StringComparison.OrdinalIgnoreCase));
        InitializePage();
    }

    private void BtnClickCreatePortfolio()
    {
        NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_ADD}?parentId={PortfolioId}");
    }

    private bool ShowContainerPreferences { get; set; } = false;

    private void ToggleContainerPreferences()
    {
        ShowContainerPreferences = !ShowContainerPreferences;
    }

    private MarketDefResp? DefaultMarket(PortfolioResp p)
    {
        return Markets?.FirstOrDefault(m => m.Id.Equals(p.Metadata?.DefaultMarketId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    private async void SwitchToSavedTab()
    {
        var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
        var savedTab = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "MyPortfolioDetails-active-tab");
        ActiveTab = string.IsNullOrEmpty(savedTab) ? TabIdSummary : savedTab;
        if (ActiveTab != TabIdSummary && ActiveTab != TabIdPositions && ActiveTab != TabIdTxBuysSells && ActiveTab != TabIdTxSettled
            && ActiveTab != TabIdSnapshots && ActiveTab != TabIdTrends && ActiveTab != TabIdPreferences)
        {
            ActiveTab = TabIdSummary;
        }
    }

    private async void SwitchTab(string tab)
    {
        CloseAlert();
        if (tab == TabIdPreferences)
        {
            // the preferences tab is transient; don't persist it as the active tab
            return;
        }
        var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
        await jsLocalStorage.InvokeVoidAsync("LocalStoreSet", "MyPortfolioDetails-active-tab", tab);
    }
}
