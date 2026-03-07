using MyPo.Libs.Clavis;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class ServicesBootstrapper
{
	private static readonly ILogger<ServicesBootstrapper> logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ServicesBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		ConfigureClavis(appBuilder.Services);
	}

	private static void ConfigureClavis(IServiceCollection services)
	{
		logger.LogInformation("Configuring Clavis service...");

		var key = Environment.GetEnvironmentVariable("CLAVIS_KEY") ?? null;
		if (string.IsNullOrEmpty(key))
		{
			logger.LogWarning("Clavis key not found in environment variables. A random key will be created!");
			key = Guid.NewGuid().ToString();
		}
		if (key.Length < 32)
		{
			logger.LogWarning("Clavis key length is less than 32 characters. It will be padded with random characters!");
			while (key.Length < 32) key = key.PadRight(1, Guid.NewGuid().ToString()[0]);
		}
		services.AddSingleton<Clavis, Clavis>(sp => new Clavis(key));
	}
}
