using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CStockChart : BaseComponent
{
	[Parameter]
	public MarketDef? Market { get; set; }

	[Parameter]
	public string? Symbol { get; set; }

	private string? ErrorMessage {get; set; }

	private readonly HashSet<string> ChartsUseVietstock = ["HOSE", "HNX", "UPCOM"];

	private bool UseVietstockChart => ChartsUseVietstock.Contains(Market?.Code ?? "");

	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		ErrorMessage = null;
		if (Market == null || String.IsNullOrEmpty(Symbol))
		{
			ErrorMessage = "No Chart Available.";
		}
		if (!UseVietstockChart)
		{
			ErrorMessage = $"Charting for market '{Market?.Id}' is not supported.";
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
	}
}
