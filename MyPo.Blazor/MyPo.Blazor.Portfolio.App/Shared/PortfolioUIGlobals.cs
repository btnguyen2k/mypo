using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Shared;

public class PortfolioUIGlobals : UIGlobals
{
	public const string ROUTE_PORTFOLIO_MARKETS = $"{ROUTE_BASE}/markets";
	public const string ROUTE_PORTFOLIO_TOOL_BUY_SELL_WITH_FEE = $"{ROUTE_BASE}/tool_calc_buy_sell";

	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO = $"{ROUTE_BASE}/my_portfolio";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_ADD = $"{ROUTE_BASE}/my_portfolio/add";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_MODIFY = $"{ROUTE_BASE}/my_portfolio/modify/{{id}}";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_DELETE = $"{ROUTE_BASE}/my_portfolio/delete/{{id}}";
}
