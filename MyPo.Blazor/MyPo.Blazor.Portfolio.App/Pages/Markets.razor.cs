using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Markets : BasePage
{
	private IEnumerable<MarketDefResp>? MarketsList { get; set; }

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
				var (alertType, alertMessage) = GetPassedMessageFromQuery();
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
					ShowAlert(alertType, alertMessage, 5000);
				else
					CloseAlert();
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Unknown error");
			}
		}
	}
}
