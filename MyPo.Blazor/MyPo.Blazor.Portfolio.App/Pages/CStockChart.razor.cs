using Microsoft.AspNetCore.Components;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CStockChart : BaseComponent
{
	[Parameter]
	public string? Symbol { get; set; }

	[Parameter]
	public string? Exchange { get; set; }

	private string? ErrorMessage {get; set; }

	private readonly HashSet<string> ChartsUseVietstock = ["*VNVN", "HOSE", "HNX", "UPCOM"];

	private bool UseVietstockChart => ChartsUseVietstock.Contains(Exchange??"");
	private bool UseTradingViewChart => !UseVietstockChart;

	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			ErrorMessage = null;
			if (string.IsNullOrEmpty(Symbol))
			{
				ErrorMessage = "No Chart Available.";
			}
			else if (!UseVietstockChart && !UseTradingViewChart)
			{
				ErrorMessage = $"Charting for exchange '{Exchange}' is not supported.";
			}
			StateHasChanged();
		}
	}
}
