using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Markets : BasePage
{
	private IEnumerable<MarketDefResp>? MarketsList { get; set; }
	private Dictionary<string, MarketDefResp>? MarketsMap { get; set; }
	private MarketDefResp? SelectedMarket { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			ShowAlert("info", "Loading markets metadata...");
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var result = await apiClient.GetMarketsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (result.Status == 200)
			{
				HideUI = false;
				MarketsList = result.Data ?? [];
				MarketsMap = MarketsList.ToDictionary(m => $"{m.Country} / {m.Code}");
				var queryParameters = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
				var alertMessage = queryParameters.TryGetValue("alertMessage", out var alertMessageValue) ? alertMessageValue.ToString() : string.Empty;
				var alertType = queryParameters.TryGetValue("alertType", out var alertTypeValue) ? alertTypeValue.ToString() : string.Empty;
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage);
				}
				else
				{
					CloseAlert();
				}
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Unknown error");
			}
		}
	}
}
