using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi
{
	private CModal ModalDialogUpdateRecord { get; set; } = default!;

	private void PrepareUpdateRecord(RoiRecResp rec)
	{
		Rec = NewRoiRecReqFrom(rec);
		TxTime = Rec.TxTime.ToString(TX_DATETIME_FORMAT);
		RecId = rec.Id;
		CloseAlert();
	}

	private void BtnClickUpdateRecord(string rid)
	{
		RoiRecResp? selectedRec = RoiRecordsMap.TryGetValue(rid, out var rec) ? rec : null;
		if (selectedRec != null)
		{
			PrepareUpdateRecord(selectedRec.Value);
			ModalDialogUpdateRecord.Open();
		}
		else
		{
			ShowAlert("danger", $"ROI record '{rid}' not found.");
		}
	}

	private void BtnClickUpdateRecordClose()
	{
		ModalDialogUpdateRecord.Close();
		CloseAlert();
	}

	// private async void BtnClickUpdateTxSave()
	// {
	// 	if (!ValidateTx())
	// 	{
	// 		return;
	// 	}

	// 	var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
	// 	var resp = await apiClient.UpdateTransactionAsync(TxId, Tx, await GetAuthTokenAsync(), ApiBaseUrl);
	// 	if (resp.Status != 200)
	// 	{
	// 		ShowAlert("danger", resp.Message ?? $"Error updating transaction '{TxId}'.");
	// 		return;
	// 	}
	// 	ShowAlert("success", "Transaction updated successfully. Navigating to portfolio page...");
	// 	var passAlertMessage = $"Transaction '{TxId}' updated successfully.";
	// 	var passAlertType = "success";
	// 	await Task.Delay(500);
	// 	ModalDialogUpdateTx.Close();
	// 	CloseAlert();
	// 	var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
	// 		+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
	// 		+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
	// 		+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
	// 	NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	// }
}
