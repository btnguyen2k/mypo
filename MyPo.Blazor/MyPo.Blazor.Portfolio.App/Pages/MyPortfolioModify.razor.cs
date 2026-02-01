using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioModify : BasePage
{
	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	private PortfolioResp? SelectedPortfolio { get; set; }

	private string ParentPortfolioId { get; set; } = string.Empty;
	private string Name { get; set; } = string.Empty;
	private string Description { get; set; } = string.Empty;
	private string Currency { get; set; } = string.Empty;
	private bool IsActive { get; set; } = true;
	private string Viewers { get; set; } = string.Empty;

	private IEnumerable<PortfolioResp> MyPortfolioTree = [];

	private async Task<PortfolioResp?> LoadPortfolioAsync(string id, string authToken)
	{
		HideUI = true;
		var errorMsg = $"Portfolio '{id}' not found.";
		ShowAlert("info", "Loading portfolio, please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.GetMyPortfoliosAsync(authToken, ApiBaseUrl);
		if (result.Status != 200)
		{
			errorMsg = result.Message ?? "Error loading portfolio.";
		}
		else
		{
			var allPortfolios = result.Data ?? [];
			MyPortfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
			var myPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
			if (myPortfolioMap.TryGetValue(id, out var portfolio))
			{
				return portfolio;
			}
		}
		ShowAlert("danger", errorMsg);
		return null;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			SelectedPortfolio = await LoadPortfolioAsync(PortfolioId, await GetAuthTokenAsync());
			if (SelectedPortfolio == null)
			{
				ShowAlert("danger", "Portfolio not found.");
				return;
			}
			if (SelectedPortfolio.OwnerUserId != CurrentUser?.Id)
			{
				ShowAlert("danger", "You do not have permission to modify this portfolio.");
				return;
			}
			ParentPortfolioId = SelectedPortfolio.ParentId ?? string.Empty;
			Name = SelectedPortfolio.Name;
			Description = SelectedPortfolio.Description ?? string.Empty;
			Currency = SelectedPortfolio.Currency;
			IsActive = SelectedPortfolio.IsActive;
			Viewers = string.Join(", ", SelectedPortfolio.Metadata?.Viewers?.ToList() ?? []);

			HideUI = false;
			CloseAlert();
		}
	}

	private void BtnClickCancel()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO);
	}

	private async Task BtnClickSave()
	{
		HideUI = true;
		ShowAlert("info", "Please wait...");

		// Validate name
		if (string.IsNullOrWhiteSpace(Name))
		{
			HideUI = false;
			ShowAlert("warning", "Name is required.");
			return;
		}

		// Validate currency
		if (string.IsNullOrWhiteSpace(Currency))
		{
			HideUI = false;
			ShowAlert("warning", "Currency is required.");
			return;
		}

		var metadata = SelectedPortfolio!.Metadata ?? new();
		metadata.Viewers = new HashSet<string>(Viewers?.ToLower().Split([',',';','\t','\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);

		var req = new CreateOrUpdatePortfolioReq
		{
			Id = PortfolioId,
			Name = Name.Trim(),
			Description = Description.Trim(),
			Currency = Currency.ToUpper().Trim(),
			ParentId = ParentPortfolioId,
			IsActive = IsActive,
			Metadata = metadata,
		};
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAsync(PortfolioId, req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideUI = false;
			ShowAlert("danger", resp.Message ?? "Error updating the portfolio.");
			return;
		}
		ShowAlert("success", "Portfolio updated successfully. Navigating to my portfolio page...");
		var passAlertMessage = $"Portfolio '{req.Name}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}");
	}
}
