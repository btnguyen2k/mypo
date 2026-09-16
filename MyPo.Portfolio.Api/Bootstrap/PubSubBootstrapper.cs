using JasperFx.Resources;
using MyPo.Portfolio.Api.PubSub;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.PubSub;
using MyPo.Shared.Api.Helpers;
using MyPo.Shared.Bootstrap;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper(Priority = 1100)]
public class PubSubBootstrapper
{
    private static readonly ILogger<PubSubBootstrapper> logger
        = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<PubSubBootstrapper>();

    private const string WolverineFxDbConfKey = "Databases:WolverineFx";
    private const string WolverineSchema = "wolverine";

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        var connectionString = GetPostgresqlConnectionString(appBuilder.Configuration, WolverineFxDbConfKey);
        var durable = !string.IsNullOrWhiteSpace(connectionString);

        appBuilder.Services.AddScoped<IPubSubPublisher, WolverinePubSubPublisher>();
        if (durable)
        {
            appBuilder.Services.AddScoped<IPortfolioDeletionService, DurablePortfolioDeletionService>();
        }
        else
        {
            appBuilder.Services.AddScoped<IPortfolioDeletionService, PortfolioDeletionService>();
        }

        // Handlers in the specified assembly will be discovered automatically
        appBuilder.Host.UseWolverine(options =>
        {
            options.Discovery.IncludeAssembly(typeof(PubSubBootstrapper).Assembly);

            if (durable)
            {
                options.PersistMessagesWithPostgresql(connectionString!, WolverineSchema);
                options.UseEntityFrameworkCoreTransactions();
                options.Policies.UseDurableLocalQueues();
            }
        });

        if (durable)
        {
            appBuilder.Host.UseResourceSetupOnStartup();
            logger.LogInformation(
                "Configured Wolverine pub/sub with PostgreSQL persistence in schema {Schema}.",
                WolverineSchema);
        }
        else
        {
            logger.LogWarning(
                "Configured Wolverine pub/sub with non-durable local queues because {ConfigurationKey} is not PostgreSQL.",
                WolverineFxDbConfKey);
        }
    }

    private static string? GetPostgresqlConnectionString(IConfiguration configuration, string confKey)
    {
        var dbConf = configuration.GetSection(confKey).Get<DbConf>();
        if (dbConf?.Type is not (DbType.PGSQL or DbType.POSTGRESQL))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dbConf.ConnectionString))
        {
            throw new InvalidDataException(
                $"No connection string name found at key '{confKey}:ConnectionString' in the configurations.");
        }

        var connectionString = configuration.GetConnectionString(dbConf.ConnectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidDataException(
                $"No connection string '{dbConf.ConnectionString}' defined in the ConnectionStrings section in the configurations.");
        }

        return connectionString;
    }
}
