
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Layout;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Shared.Bootstrap;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper]
public class MenuBootstrapper
{
	public static void ConfigureServices(IServiceCollection _)
	{
		Sidebar.AddOrReplaceEntry(new Sidebar.SidebarItem
		{
			Id = "markets",
			Label = "Markets",
			Icon = "bi-bank",
			Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MARKETS,
		});

		Sidebar.AddOrReplaceEntry(new Sidebar.SidebarSection
		{
			Id = "portfolio",
			Label = "Portfolio",
			Items = [
				// new Sidebar.SidebarItem
				// {
				// 	Id = "markets",
				// 	Label = "Markets",
				// 	Icon = "bi-bank",
				// 	Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MARKETS,
				// }
			],
		});
	}
}
