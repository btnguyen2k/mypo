using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
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
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to update ROI records in this portfolio.");
			return;
		}
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
		ModalDialogUpdateRecord.CloseAlert();
	}

	private async void BtnClickUpdateRecordSave()
	{
		ModalDialogUpdateRecord.ShowAlert("info", "Saving ROI record...");
		if (!ValidateRoiRec())
		{
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioRoiRecAsync(Rec, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogUpdateRecord.ShowAlert("danger", resp.Message ?? $"Error updating ROI record '{RecId}'.");
			return;
		}
		ModalDialogUpdateRecord.ShowAlert("success", "ROI record updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"ROI record '{RecId}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogUpdateRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
