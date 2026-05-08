using System.Net;
using MyPo.Blazor.App.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyPo.Blazor.App.Pages.ExternalAuth;

public abstract class LoginExternalBase : BaseComponent
{
	protected string AlertType { get; set; } = string.Empty;
	protected string AlertTitle { get; set; } = string.Empty;
	protected string AlertMessage { get; set; } = string.Empty;
	protected bool ShowReturnLinks { get; set; } = false;

	[Inject]
	protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

	protected void ShowAlert(string type, string message, string title = "")
	{
		AlertType = type;
		AlertTitle = title;
		AlertMessage = message;
		StateHasChanged();
	}

	protected IDictionary<string, string> ParseQueryParams(bool urlDecode = true, bool htmlDecode = false)
	{
		var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
		var queryParams = QueryHelpers.ParseQuery(uri.Query);
		return queryParams.ToDictionary(x => x.Key, x =>
		{
			var value = x.Value.ToString();
			if (urlDecode)
			{
				value = WebUtility.UrlDecode(value);
			}
			if (htmlDecode)
			{
				value = WebUtility.HtmlDecode(value);
			}
			return value;
		}) ?? [];
	}

	protected void ForceLoad(string url)
	{
		NavigationManager.NavigateTo(url, forceLoad: true);
	}
}
