using MyPo.Demo.Shared.Models;
using MyPo.Shared.Api.Helpers;
using MyPo.Shared.Bootstrap;
using MyPo.Shared.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace MyPo.Demo.Api.Bootstrap;

/// <summary>
/// Built-in bootstrapper that initializes and registers cache services.
/// </summary>
[Bootstrapper]
public class CacheBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<CacheBootstrapper>();

		var (confKeyBase, keyedServiceName) = ("Caches:Application", nameof(Application));
		logger.LogInformation("Configuring cache service {confKey}...", confKeyBase);
		var cacheConf = CacheBootstrapHelper.SetupCache(appBuilder, confKeyBase, keyedServiceName, logger);
		if (cacheConf != null)
		{
			appBuilder.Services.AddSingleton<ICacheFacade<Application>>(sp =>
			{
				var options = new CacheFacadeOptions()
				{
					CompressionLevel = cacheConf.CompressionLevel,
					KeyPrefix = cacheConf.KeyPrefix,
					DefaultDistributedCacheEntryOptions = new DistributedCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = cacheConf.ExpirationAfterWrite > 0 ? TimeSpan.FromSeconds(cacheConf.ExpirationAfterWrite) : null,
						SlidingExpiration = cacheConf.ExpirationAfterAccess > 0 ? TimeSpan.FromSeconds(cacheConf.ExpirationAfterAccess) : null,
					},
				};
				var cacheService = sp.GetRequiredKeyedService<IDistributedCache>(keyedServiceName);
				return new CacheFacade<Application>(cacheService, options);
			});
		}

		logger.LogInformation("Cache services configured.");
	}
}
