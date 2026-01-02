using MyPo.Blazor.App.Layout;
using MyPo.Blazor.Demo.App.Shared;
using MyPo.Shared.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace MyPo.Blazor.Demo.App.Bootstrap;

[Bootstrapper]
public class MenuBootstrapper
{
	public static void ConfigureServices(IServiceCollection _)
	{
		Sidebar.AddOrReplaceSection(new Sidebar.SidebarSection
		{
			Id = "demo",
			Label = "Demo",
			Items = [
				new Sidebar.SidebarItem
				{
					Id = "apps",
					Label = "Applications",
					Icon = "cil-apps",
					Url = DemoUIGlobals.ROUTE_APPLICATIONS,
				}
			],
		});
	}
}
