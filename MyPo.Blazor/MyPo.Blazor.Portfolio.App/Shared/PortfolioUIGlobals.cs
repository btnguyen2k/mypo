using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Shared;

public class PortfolioUIGlobals : UIGlobals
{
	public const int AFTER_ACTION_DELAY_MS = 750;

	public const string ROUTE_PORTFOLIO_MARKETS = $"{ROUTE_BASE}/markets";
	public const string ROUTE_PORTFOLIO_TOOL_BUY_SELL_WITH_FEE = $"{ROUTE_BASE}/tool_calc_buy_sell";
	public const string ROUTE_PORTFOLIO_TOOL_PRICE_RUN = $"{ROUTE_BASE}/tool_price_run";

	public const string ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO_EMPTY = $"{ROUTE_BASE}/stock_symbol/";
	public const string ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO = $"{ROUTE_BASE}/stock_symbol/{{Symbol}}";

	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO = $"{ROUTE_BASE}/my_portfolio";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS = $"{ROUTE_BASE}/my_portfolio/{{PortfolioId}}";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_ADD = $"{ROUTE_BASE}/my_portfolio/add";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_MODIFY = $"{ROUTE_BASE}/my_portfolio/modify/{{PortfolioId}}";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_DELETE = $"{ROUTE_BASE}/my_portfolio/delete/{{PortfolioId}}";

	public const string ROUTE_PORTFOLIO_MY_PREFERENCES = $"{ROUTE_BASE}/my_preferences";

	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS = $"{ROUTE_BASE}/my_portfolio_plans";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_VIEW = $"{ROUTE_BASE}/my_portfolio_plans/{{PlanId}}";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_ADD = $"{ROUTE_BASE}/my_portfolio_plans/add";
	public const string ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_EDIT = $"{ROUTE_BASE}/my_portfolio_plans/edit/{{PlanId}}";

	public static List<TimeZoneGroup> TIME_ZONE_GROUPS = [
		new TimeZoneGroup()
		{
			GroupName = "Americas",
			TimeZones = [
				new TimeZoneItem() { Id = "America/New_York", Label = "Eastern Time - New York (UTC-5/-4)" },
				new TimeZoneItem() { Id = "America/Chicago", Label = "Central Time - Chicago (UTC-6/-5)" },
				new TimeZoneItem() { Id = "America/Denver", Label = "Mountain Time - Denver (UTC-7/-6)" },
				new TimeZoneItem() { Id = "America/Los_Angeles", Label = "Pacific Time - Los Angeles (UTC-8/-7)" },
				new TimeZoneItem() { Id = "America/Anchorage", Label = "Alaska Time (UTC-9/-8)" },
				new TimeZoneItem() { Id = "Pacific/Honolulu", Label = "Hawaii Time (UTC-10)" },
				new TimeZoneItem() { Id = "America/Sao_Paulo", Label = "Brazil - São Paulo (UTC-3)" },
				new TimeZoneItem() { Id = "America/Toronto", Label = "Canada - Toronto (UTC-5/-4)" },
				new TimeZoneItem() { Id = "America/Vancouver", Label = "Canada - Vancouver (UTC-8/-7)" },
			]
		},
		new TimeZoneGroup()
		{
			GroupName = "Europe",
			TimeZones = [
				new TimeZoneItem() { Id = "Europe/London", Label = "UK - London (UTC±0/+1)" },
				new TimeZoneItem() { Id = "Europe/Paris", Label = "Central Europe - Paris (UTC+1/+2)" },
				new TimeZoneItem() { Id = "Europe/Berlin", Label = "Central Europe - Berlin (UTC+1/+2)" },
				new TimeZoneItem() { Id = "Europe/Moscow", Label = "Russia - Moscow (UTC+3)" },
			]
		},
		new TimeZoneGroup()
		{
			GroupName = "Asia & Pacific",
			TimeZones = [
				new TimeZoneItem() { Id = "Asia/Dubai", Label = "UAE - Dubai (UTC+4)" },
				new TimeZoneItem() { Id = "Asia/Kolkata", Label = "India - Mumbai/Delhi (UTC+5:30)" },
				new TimeZoneItem() { Id = "Asia/Singapore", Label = "Singapore (UTC+8)" },
				new TimeZoneItem() { Id = "Asia/Hong_Kong", Label = "Hong Kong (UTC+8)" },
				new TimeZoneItem() { Id = "Asia/Shanghai", Label = "China - Shanghai (UTC+8)" },
				new TimeZoneItem() { Id = "Asia/Tokyo", Label = "Japan - Tokyo (UTC+9)" },
				new TimeZoneItem() { Id = "Asia/Seoul", Label = "South Korea - Seoul (UTC+9)" },
				new TimeZoneItem() { Id = "Australia/Sydney", Label = "Australia - Sydney (UTC+10/+11)" },
				new TimeZoneItem() { Id = "Pacific/Auckland", Label = "New Zealand - Auckland (UTC+12/+13)" },
			]
		},
		new TimeZoneGroup()
		{
			GroupName = "Middle East",
			TimeZones = [
				new TimeZoneItem() { Id = "Asia/Riyadh", Label = "Saudi Arabia - Riyadh (UTC+3)" },
				new TimeZoneItem() { Id = "Asia/Tehran", Label = "Iran - Tehran (UTC+3:30/+4:30)" },
			]
		},
		new TimeZoneGroup()
		{
			GroupName = "Africa",
			TimeZones = [
				new TimeZoneItem() { Id = "Africa/Johannesburg", Label = "South Africa - Johannesburg (UTC+2)" },
				new TimeZoneItem() { Id = "Africa/Cairo", Label = "Egypt - Cairo (UTC+2/+3)" },
				new TimeZoneItem() { Id = "Africa/Lagos", Label = "Nigeria - Lagos (UTC+1)" },
			]
		}
	];
}

public sealed class TimeZoneGroup
{
	public string GroupName { get; set; } = string.Empty;
	public List<TimeZoneItem> TimeZones { get; set; } = [];
}

public sealed class TimeZoneItem
{
	public string Id { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
}
