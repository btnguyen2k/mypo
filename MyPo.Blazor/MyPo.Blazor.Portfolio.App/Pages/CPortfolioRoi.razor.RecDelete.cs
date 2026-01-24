using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi
{
	private CModal ModalDialogDeleteRecord { get; set; } = default!;

	private void PrepareDeleteRecord(RoiRecResp rec)
	{
		PrepareUpdateRecord(rec);
	}

	private void BtnClickDeleteRecord(string rid)
	{
		RoiRecResp? selectedRec = RoiRecordsMap.TryGetValue(rid, out var rec) ? rec : null;
		if (selectedRec != null)
		{
			PrepareDeleteRecord(selectedRec.Value);
			ModalDialogDeleteRecord.Open();
		}
		else
		{
			ShowAlert("danger", $"ROI record '{rid}' not found.");
		}
	}

	private void BtnClickDeleteRecordClose()
	{
		ModalDialogDeleteRecord.Close();
		CloseAlert();
	}

	private async void BtnClickDeleteRecordConfirm()
	{
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.DeleteRoiRecAsync(Rec.PortfolioId, RecId, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message ?? $"Error deleting ROI record '{RecId}'.");
			return;
		}
		ShowAlert("success", "ROI record deleted successfully. Navigating to portfolio page...");
		var passAlertMessage = $"ROI record '{RecId}' deleted successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogDeleteRecord.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
