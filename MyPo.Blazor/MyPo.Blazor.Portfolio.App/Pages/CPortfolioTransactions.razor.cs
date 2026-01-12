using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions : BaseComponent
{
	[Parameter]
	public IEnumerable<TransactionRecResp>? Transactions { get; set; }

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	private CModal ModalDialogAddTx { get; set; } = default!;

	private string TxType { get; set; } = string.Empty;
	private string TxMarket { get; set; } = string.Empty;
	private string TxTime { get; set; } = string.Empty;
	private string TxItemType { get; set; } = string.Empty;
	private string TxItemCode { get; set; } = string.Empty;
	private decimal TxItemPrice { get; set; } = 0.00m;
	private decimal TxQuantity { get; set; } = 0;
	private decimal FeeTx { get; set; } = 0.00m;
	private decimal FeeTax { get; set; } = 0.00m;
	private decimal FeeOther { get; set; } = 0.00m;
	private string TxNotes { get; set; } = string.Empty;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTransactions).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");
			// await JS.InvokeVoidAsync("InitDatetimePickers");
		}
	}

	private void BtnClickAddTx()
	{
		ModalDialogAddTx.Open();
	}
}
