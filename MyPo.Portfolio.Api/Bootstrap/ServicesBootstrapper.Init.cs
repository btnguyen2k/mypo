using Finance.Net.Interfaces;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class YFinanceInitializer(
	IServiceProvider serviceProvider,
	ILogger<YFinanceInitializer> logger) : IHostedService
{
	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Initializing YFinance client...");

		using (var scope = serviceProvider.CreateScope())
		{
			var yahooFinanceService = scope.ServiceProvider.GetRequiredService<IYahooFinanceService>();
			try
			{
				await yahooFinanceService.GetQuoteAsync("AAPL", cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error initializing YFinance client: {Message}", ex.Message);
			}
		}

		await Task.CompletedTask;
	}
}
