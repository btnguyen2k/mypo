
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
			Priority = 100,
		});
		Sidebar.AddOrReplaceEntry(new Sidebar.SidebarItem
		{
			Id = "buy_sell_calc",
			Label = "Buy/Sell Calculator",
			Icon = "cil-calculator",
			Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_TOOL_BUY_SELL_WITH_FEE,
			Priority = 101,
		});

		Sidebar.AddOrReplaceEntry(new Sidebar.SidebarSection
		{
			Id = "portfolio",
			Label = "Portfolio",
			Priority = 200,
			Items = [
				new Sidebar.SidebarItem
				{
					Id = "myportfolio",
					Label = "My Portfolio",
					Icon = "bi-building",
					Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO,
				}
			],
		});
		// Sidebar.AddOrReplaceEntry(new Sidebar.SidebarSection
		// {
		// 	Id = "tools",
		// 	Label = "Tools",
		// 	Priority = 201,
		// 	Items = [
		// 		new Sidebar.SidebarItem
		// 		{
		// 			Id = "buy_sell_calc",
		// 			Label = "Buy/Sell Calculator",
		// 			Icon = "cil-calculator",
		// 			Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_TOOL_BUY_SELL_WITH_FEE,
		// 		}
		// 	],
		// });
	}
}
