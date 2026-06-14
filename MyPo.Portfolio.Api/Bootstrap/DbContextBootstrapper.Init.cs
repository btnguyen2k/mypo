using MyPo.Portfolio.Shared.EF;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class PortfolioInitializer(
    IServiceProvider serviceProvider,
    ILogger<PortfolioInitializer> logger,
    IWebHostEnvironment environment) : IHostedService
{
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing application data...");

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>() as PortfolioDbContextRepository
                ?? throw new InvalidOperationException($"Portfolio repository is not an instance of {nameof(PortfolioDbContextRepository)}.");
            var tryParseInitDb = bool.TryParse(Environment.GetEnvironmentVariable(MyPo.Shared.Api.Globals.ENV_INIT_DB), out var initDb);
            if (environment.IsDevelopment() || (tryParseInitDb && initDb))
            {
                logger.LogInformation("Ensuring database schema exist...");
                dbContext.Database.EnsureCreated();
            }
        }

        return Task.CompletedTask;
    }
}
