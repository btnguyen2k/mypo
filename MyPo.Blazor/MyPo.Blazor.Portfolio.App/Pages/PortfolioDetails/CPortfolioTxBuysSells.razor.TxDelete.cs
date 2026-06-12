using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxBuysSells
{
    private CModal ModalDialogDeleteTx { get; set; } = default!;

    private void PrepareDeleteTx(TxBuySellResp tx)
    {
        PrepareUpdateTx(tx);
    }

    private void BtnClickDeleteTx(string tid)
    {
        if (Portfolio?.OwnerUserId != CurrentUser?.Id)
        {
            ShowAlert("danger", "You do not have permission to delete transactions from this portfolio.");
            return;
        }
        TxBuySellResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
        if (selectedTx != null)
        {
            PrepareDeleteTx(selectedTx.Value);
            ModalDialogDeleteTx.Open();
        }
        else
        {
            ShowAlert("danger", $"Transaction '{tid}' not found.");
        }
    }

    private void BtnClickDeleteTxClose()
    {
        ModalDialogDeleteTx.Close();
        ModalDialogDeleteTx.CloseAlert();
    }

    private async void BtnClickDeleteTxConfirm()
    {
        ModalDialogDeleteTx.ShowAlert("info", "Deleting transaction...");
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.DeleteMyPortfolioTxBuySellAsync(Tx.PortfolioId, TxId, await GetAuthTokenAsync(), ApiBaseUrl);
        if (resp.Status != 200)
        {
            ModalDialogDeleteTx.ShowAlert("danger", resp.Message ?? $"Error deleting transaction '{TxId}'.");
            return;
        }
        ModalDialogDeleteTx.ShowAlert("success", "Transaction deleted successfully. Navigating to portfolio page...");
        var passAlertMessage = $"Transaction '{TxId}' deleted successfully.";
        var passAlertType = "success";
        await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
        ModalDialogDeleteTx.Close();
        var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
            + $"?{BasePage.QUERY_PARM_REFRESH}=true"
            + $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
            + $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
        NavigationManager.NavigateTo(nextUrl);
    }
}
