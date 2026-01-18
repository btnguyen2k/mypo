using Finance.Net;
using Finance.Net.Extensions;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that register services used by MyPo.Portfolio module.
/// </summary>
[Bootstrapper]
public class ServicesBootstrapper
{
    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		appBuilder.Services.AddFinanceNet(new FinanceNetConfiguration
		{
			HttpTimeout = 10,
			HttpRetryCount = 2,
			AlphaVantageApiKey = "<ALPHA_VANTAGE__API_KEY>",
		});
		appBuilder.Services.AddHostedService<YFinanceInitializer>();
	}
}
