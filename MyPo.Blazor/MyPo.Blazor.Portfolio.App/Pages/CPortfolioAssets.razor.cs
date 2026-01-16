using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioAssets : BaseComponent
{
	[Parameter]
	public IEnumerable<AssetResp>? Assets { get; set; }
	private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];
	private AssetResp? SelectedAsset;
	private string AssetTags = string.Empty;

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

	private string AlertType { get; set; } = string.Empty;
	private string AlertMessage { get; set; } = string.Empty;
	protected bool AlertHasChanged {get; set; } = false;

	protected void CloseAlert()
	{
		AlertMessage = string.Empty;
		AlertHasChanged = false;
		StateHasChanged();
	}

	protected void ShowAlert(string type, string message)
	{
		var oldAlertType = AlertType;
		var oldAlertMessage = AlertMessage;
		AlertType = type;
		AlertMessage = message;
		AlertHasChanged = !String.IsNullOrEmpty(oldAlertMessage)
			&& (String.Compare(oldAlertMessage, message, MyPo.Shared.Globals.StringComparison) != 0
				|| String.Compare(oldAlertType, type, MyPo.Shared.Globals.StringComparison) != 0);
		StateHasChanged();
	}

	private CModal ModalDialogAssetInfo { get; set; } = default!;

	// private CreateOrUpdateTransactionRecReq Tx = default!;
	// private string TxTime { get; set; } = string.Empty;
	// private string TxId { get; set; } = string.Empty;

	// [Inject]
	// private IJSRuntime JS { get; set; } = default!;

	// protected override async Task OnAfterRenderAsync(bool firstRender)
	// {
	// 	if (firstRender)
	// 	{
	// 		Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTransactions).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
	// 		var module = await moduleTask.Value;
    //     	await module.InvokeAsync<string>("InitDatetimePickers");

	// 		// MarketsMap = Markets?.ToDictionary(m => m.Id, m => m) ?? [];
	// 	}
	// }

	private void BtnClickAssetInfo(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset != null)
		{
			AssetTags = SelectedAsset?.Tags ?? string.Empty;
			ModalDialogAssetInfo.Open();
		}
	}

	private async void BtnClickUpdateAssetTags()
	{
		var req = new CreateOrUpdateAssetReq()
		{
			Id = SelectedAsset!.Value.Id,
			PortfolioId = SelectedAsset!.Value.PortfolioId,
			ItemType = SelectedAsset!.Value.ItemType,
			ItemCode = SelectedAsset!.Value.ItemCode,
			Quantity = SelectedAsset!.Value.Quantity,
			AveragePrice = SelectedAsset!.Value.AveragePrice,
			MarketId = SelectedAsset!.Value.MarketId,
			Tags = AssetTags,
		};

		ShowAlert("info", "Updating asset tags...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAssetAsync(req.PortfolioId, req.Id, req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Asset tags updated successfully.");
	}
}
