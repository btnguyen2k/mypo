using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi
{
	private CModal ModalDialogAddRecord { get; set; } = default!;

	private void PrepareAddRecord()
	{
		Rec = new CreateOrUpdateRoiRecReq()
		{
			PortfolioId = PortfolioId,
			TxType = string.Empty,
			TxTime = DateTimeOffset.Now,
			RefItemType = string.Empty,
			RefItemCode = string.Empty,
			RefMarketId = string.Empty,
		};
		TxTime = Rec.TxTime.ToString(TX_DATETIME_FORMAT);
		RecId = string.Empty;
		CloseAlert();
	}

	private void BtnClickAddRecord()
	{
		PrepareAddRecord();
		ModalDialogAddRecord.Open();
	}

	private void BtnClickAddRecordClose()
	{
		ModalDialogAddRecord.Close();
		CloseAlert();
	}

	private async void BtnClickAddRecordSave()
	{
		ShowAlert("info", "Adding ROI record...");
		if (!ValidateRoiRec())
		{
			return;
		}

		Rec.PortfolioId = PortfolioId;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreateMyPortfolioRoiRecAsync(Rec, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message ?? "Failed to create ROI record.");
			return;
		}
		ShowAlert("success", "ROI record added successfully. Navigating to portfolio page...");
		var passAlertMessage = $"ROI record '{resp.Data.Id}' added successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogAddRecord.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
