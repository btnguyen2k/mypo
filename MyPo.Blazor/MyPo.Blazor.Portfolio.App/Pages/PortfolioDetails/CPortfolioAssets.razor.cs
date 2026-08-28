using FinHub.Client.Models.Stocks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioAssets : CBase
{
    [Parameter]
    public IEnumerable<AssetResp>? Assets { get; set; }
    private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];
    private AssetResp? SelectedAsset;
    private decimal TotalCost => Assets?.Where(a => a.Market?.Currency == Portfolio?.Currency).Sum(a => a.AveragePrice * a.Quantity) ?? 0;
    private decimal TotalMarketValue => Assets?.Where(a => a.Market?.Currency == Portfolio?.Currency).Sum(a =>
    {
        if (MarketPricesMap.TryGetValue(a.Id, out var latestPrice))
        {
            return latestPrice * a.Quantity;
        }
        return 0;
    }) ?? 0;
    private decimal TotalUnsettledPnl => TotalCost > 0 && TotalMarketValue > 0 ? TotalMarketValue - TotalCost : 0;
    private string TotalUnsettledPnlTextClass => TotalUnsettledPnl switch
    {
        > 0 => "text-success",
        < 0 => "text-danger",
        _ => "text-muted",
    };
    private string TotalUnsettledPnlBorderClass => TotalUnsettledPnlTextClass switch
    {
        "text-success" => "border-start-success",
        "text-danger" => "border-start-danger",
        _ => "border-start-secondary",
    };

    [Parameter]
    public Dictionary<string, StockQuote>? QuotesMap { get; set; } // map {asset-id --> quote}

    private Dictionary<string, decimal> MarketPricesMap { get; set; } = []; // map {asset-id --> market-price}

    private Dictionary<string, decimal> UnsettledPnLMap { get; set; } = []; // map {asset-id --> unsettled-p/l}

    private Dictionary<string, decimal> UnsettledPnLPercentMap { get; set; } = []; // map {asset-id --> unsettled-p/l-percent}

    private enum AssetSortColumn
    {
        Item,
        Units,
        AveragePrice,
        TotalCost,
        MarketPrice,
        MarketValue,
        UnsettledPnlPercent,
    }

    private AssetSortColumn SortColumn { get; set; } = AssetSortColumn.Item;
    private bool SortDescending { get; set; }

    private IEnumerable<AssetResp> SortedAssets
    {
        get
        {
            var assets = Assets?.Where(asset => !ShowOnlyOpenPositions || asset.Quantity > 0)
                ?? Enumerable.Empty<AssetResp>();
            IOrderedEnumerable<AssetResp> sorted = (SortColumn, SortDescending) switch
            {
                (AssetSortColumn.Item, false) => assets.OrderBy(asset => asset.ItemCode, StringComparer.OrdinalIgnoreCase),
                (AssetSortColumn.Item, true) => assets.OrderByDescending(asset => asset.ItemCode, StringComparer.OrdinalIgnoreCase),
                (AssetSortColumn.Units, false) => assets.OrderBy(asset => asset.Quantity),
                (AssetSortColumn.Units, true) => assets.OrderByDescending(asset => asset.Quantity),
                (AssetSortColumn.AveragePrice, false) => assets.OrderBy(asset => asset.AveragePrice),
                (AssetSortColumn.AveragePrice, true) => assets.OrderByDescending(asset => asset.AveragePrice),
                (AssetSortColumn.TotalCost, false) => assets.OrderBy(asset => asset.TotalCost),
                (AssetSortColumn.TotalCost, true) => assets.OrderByDescending(asset => asset.TotalCost),
                (AssetSortColumn.MarketPrice, false) => assets.OrderBy(MarketPriceForSort),
                (AssetSortColumn.MarketPrice, true) => assets.OrderByDescending(MarketPriceForSort),
                (AssetSortColumn.MarketValue, false) => assets.OrderBy(MarketValueForSort),
                (AssetSortColumn.MarketValue, true) => assets.OrderByDescending(MarketValueForSort),
                (AssetSortColumn.UnsettledPnlPercent, false) => assets.OrderBy(UnsettledPnlPercentForSort),
                (AssetSortColumn.UnsettledPnlPercent, true) => assets.OrderByDescending(UnsettledPnlPercentForSort),
                _ => assets.OrderBy(asset => asset.ItemCode, StringComparer.OrdinalIgnoreCase),
            };
            return sorted.ThenBy(asset => asset.ItemCode, StringComparer.OrdinalIgnoreCase);
        }
    }

    private string AssetTags = string.Empty;

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }
    private MarketDefResp? DefaultMarket => Markets?.FirstOrDefault(m => m.Id == Portfolio?.Metadata?.DefaultMarketId);

    private bool ShowOnlyOpenPositions { get; set; } = true;

    private CModal ModalDialogAssetUpdateTags { get; set; } = default!;
    private CModal ModalDialogBuySellAssetCalculator { get; set; } = default!;

    private decimal MarketPriceForSort(AssetResp asset)
    {
        return MarketPricesMap.GetValueOrDefault(asset.Id);
    }

    private decimal MarketValueForSort(AssetResp asset)
    {
        return MarketPriceForSort(asset) * asset.Quantity;
    }

    private decimal UnsettledPnlPercentForSort(AssetResp asset)
    {
        return UnsettledPnLPercentMap.GetValueOrDefault(asset.Id);
    }

    private void SortBy(AssetSortColumn column)
    {
        if (SortColumn == column)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = column;
            SortDescending = false;
        }
    }

    private string SortIcon(AssetSortColumn column)
    {
        return SortColumn != column
            ? "bi-arrow-down-up"
            : SortDescending ? "bi-caret-down-fill" : "bi-caret-up-fill";
    }

    private string AriaSort(AssetSortColumn column)
    {
        return SortColumn != column
            ? "none"
            : SortDescending ? "descending" : "ascending";
    }

    private int MarketStatus(AssetResp? asset)
    {
        return asset == null || asset.Quantity <= 0
            ? 0
            : MarketPricesMap.TryGetValue(asset.Id, out var lp)
                ? lp == asset.AveragePrice
                    ? 0
                    : lp > asset.AveragePrice
                        ? 1
                        : -1
                : 0;
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
                    var unsettledPnL = (latestPrice - asset.AveragePrice) * asset.Quantity;
                    UnsettledPnLMap[asset.Id] = unsettledPnL;
                    UnsettledPnLPercentMap[asset.Id] = FormatUtils.CalculatePercentageChange(
                        asset.AveragePrice,
                        latestPrice
                    );
                }
            }
            await InvokeAsync(StateHasChanged);
        }
    }

    private void BtnClickAssetInfo(string assetId)
    {
        SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
        if (SelectedAsset == null)
        {
            ShowAlert("danger", "Asset not found.");
            return;
        }
        SelectedAsset.Market = Markets?.FirstOrDefault(m => m.Id == SelectedAsset.MarketId);
        var ticker = $"{SelectedAsset.Market?.Code}:{SelectedAsset.ItemCode}";
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO.Replace("{Symbol}", ticker, StringComparison.OrdinalIgnoreCase)}?pid={SelectedAsset.PortfolioId}";
        NavigationManager.NavigateTo(nextUrl);
    }

    private void BtnClickAssetBuySellCalculator(string assetId)
    {
        SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
        if (SelectedAsset != null)
        {
            ModalDialogBuySellAssetCalculator.CloseAlert();
            ModalDialogBuySellAssetCalculator.Open();
        }
    }

    private void BtnClickAssetUpdateTags(string assetId)
    {
        SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
        if (SelectedAsset == null)
        {
            ShowAlert("danger", "Asset not found.");
            return;
        }
        AssetTags = string.Join(", ", SelectedAsset.Metadata?.Tags ?? new HashSet<string>());
        ModalDialogAssetUpdateTags.CloseAlert();
        ModalDialogAssetUpdateTags.Open();
    }

    private async void BtnClickAssetUpdateTagsConfirmed()
    {
        var req = new CreateOrUpdateAssetReq()
        {
            Id = SelectedAsset!.Id,
            PortfolioId = SelectedAsset!.PortfolioId,
            ItemType = SelectedAsset!.ItemType,
            ItemCode = SelectedAsset!.ItemCode,
            Quantity = SelectedAsset!.Quantity,
            AveragePrice = SelectedAsset!.AveragePrice,
            MarketId = SelectedAsset!.MarketId,
            Metadata = SelectedAsset!.Metadata ?? new AssetMetadata(),
        };
        req.Metadata.Tags = AssetTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

        ModalDialogAssetUpdateTags.ShowAlert("info", "Updating asset tags...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.UpdateMyPortfolioAssetAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
        if (resp.Status != 200)
        {
            ModalDialogAssetUpdateTags.ShowAlert("danger", resp.Message ?? "Error updating asset tags.");
            return;
        }
        ModalDialogAssetUpdateTags.ShowAlert("success", "Asset tags updated successfully.");
        await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
        ModalDialogAssetUpdateTags.Close();
        var passAlertMessage = $"{SelectedAsset!.ItemCode}'s tags updated successfully.";
        var passAlertType = "success";
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio?.Id, StringComparison.OrdinalIgnoreCase)}"
            + $"?{BasePage.QUERY_PARM_REFRESH}=true"
            + $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
            + $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
        NavigationManager.NavigateTo(nextUrl);
    }
}
