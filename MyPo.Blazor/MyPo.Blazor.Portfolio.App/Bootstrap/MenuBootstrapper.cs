
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
        	Id = "events",
        	Label = "Events",
        	Icon = "bi-calendar-event",
        	Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_EVENTS,
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
                    Icon = "bi-briefcase",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO,
                },
                new Sidebar.SidebarItem
                {
                    Id = "myportfolioplans",
                    Label = "My Portfolio Plans",
                    Icon = "bi-distribute-vertical",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS,
                },
                new Sidebar.SidebarItem
                {
                    Id = "mypreferences",
                    Label = "My Preferences",
                    Icon = "bi-sliders",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PREFERENCES,
                }
            ],
        });
        Sidebar.AddOrReplaceEntry(new Sidebar.SidebarSection
        {
            Id = "tools",
            Label = "Tools",
            Priority = 201,
            Items = [
                new Sidebar.SidebarItem
                {
                    Id = "stock_symbol_info",
                    Label = "Stock Symbol Info",
                    Icon = "bi-graph-up-arrow",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO_EMPTY,
                },
                new Sidebar.SidebarItem
                {
                    Id = "tool_buy_sell_calc",
                    Label = "Buy/Sell Calculator",
                    Icon = "cil-calculator",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_TOOL_BUY_SELL_WITH_FEE,
                },
                new Sidebar.SidebarItem
                {
                    Id = "tool_price_run",
                    Label = "Price Run",
                    Icon = "cil-running",
                    Url = PortfolioUIGlobals.ROUTE_PORTFOLIO_TOOL_PRICE_RUN,
                }
            ],
        });
    }
}
