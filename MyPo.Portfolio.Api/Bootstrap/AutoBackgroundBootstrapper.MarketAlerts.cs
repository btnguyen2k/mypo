using MyPo.Libs.Tempus;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Identity;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class AutoBackgroundSendMarketAlerts : AutoBackgroundAnnouncementScanner
{
	public AutoBackgroundSendMarketAlerts(
			IServiceProvider serviceProvider, ILogger<AutoBackgroundSendMarketAlerts> logger
		) : base(serviceProvider, logger)
	{
	}

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// delay a bit to avoid all instances running at the same time after deployment or restart
		await Task.Delay(Random.Shared.Next(10000, 30000), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
			using (var scope = ServiceProvider.CreateScope())
			try
			{
				var identityRepository = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
				var allUsers = await identityRepository.GetAllUsersAsync(cancellationToken: cancellationToken);
				foreach (var user in allUsers)
				try
				{
					var userId = user.UserName!.ToLower();
					Logger.LogInformation("Processing market alerts for user {userId}...", userId);
					if (!(user.Metadata?.MarketAlertViaTelegram??false))
					{
						Logger.LogInformation("User {userId} has not enabled market alerts via Telegram. Skipping...", userId);
						continue;
					}
					if (string.IsNullOrEmpty(user.Metadata?.GetTelegramBotApiKey()??string.Empty))
					{
						Logger.LogInformation("User {userId} has not configured Telegram bot API key for market alerts. Skipping...", userId);
						continue;
					}
					if (!(user.Metadata?.GetTelegramChatIDs()??[]).Any())
					{
						Logger.LogInformation("User {userId} has not configured any Telegram chat IDs for market alerts. Skipping...", userId);
						continue;
					}
					var now = DateTimeOffset.Now.ToTimeZoneSilently(user.Metadata?.MarketAlertTimezone??"");
					if (now == null)
					{
						Logger.LogInformation("User {userId} has an invalid market alert timezone configured. Skipping...", userId);
						continue;
					}
					if (!now.Value.WithinDowList(user.Metadata?.MarketAlertDaysOfWeek??[]))
					{
						Logger.LogInformation("Today is {dayOfWeek}, which is now in the user's market alert time window. Skipping...", now.Value.DayOfWeek.ToString());
					}
					if (!now.Value.WithinTimeWindow(user.Metadata?.MarketAlertStartTime??TimeOnly.MinValue, user.Metadata?.MarketAlertEndTime??TimeOnly.MaxValue))
					{
						Logger.LogInformation("Current time {currentTime} is not within the user's market alert time window. Skipping...", now.Value.ToString("HH:mm"));
						continue;
					}
					var checkpoint = await GetOrInitCheckpoint(
						ownerId: userId,
						portfolioId: CheckpointEntity.NON_PORTFOLIO,
						marketId: CheckpointEntity.NON_MARKET,
						itemCode: CheckpointEntity.NON_ITEM,
						checkpointType: CheckpointEntity.CHECKPOINT_MARKET_ALERTS,
						cancellationToken
					);
					var alertDelay = TimeSpan.FromMinutes(user.Metadata?.MarketAlertDelayMinutes??60);
					if (checkpoint == null || (checkpoint.CheckpointTime != DateTimeOffset.MinValue && DateTimeOffset.UtcNow-checkpoint.CheckpointTime < alertDelay))
					{
						Logger.LogInformation("Checkpoint for user {userId} is not ready for sending market alerts. Last checkpoint time: {checkpointTime}, alert delay: {alertDelay}. Skipping...", userId, checkpoint?.CheckpointTime.ToString("o")??"null", alertDelay);
						continue;
					}

					var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
					var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
					var teleBot = new TelegramBotClient(user.Metadata!.GetTelegramBotApiKey()!);
					await MarketAlertDividendEvents(portfolioRepo, finHubClient, teleBot, user.Metadata.GetTelegramChatIDs()??[], cancellationToken);
					await MarketAlertNewListings(portfolioRepo, finHubClient, teleBot, user.Metadata.GetTelegramChatIDs()??[], cancellationToken);

					checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
					var dbresult = await portfolioRepo.UpdateCheckpointAsync(checkpoint, cancellationToken);
					if (dbresult == null)
					{
						Logger.LogError(
							"Failed to update checkpoint: Owner: {owner} - Portfolio: {portfolio} - Market: {market} - Item: {item} - Type: {type}.",
							checkpoint.OwnerId, checkpoint.PortfolioId, checkpoint.MarketId, checkpoint.ItemCode, checkpoint.CheckpointType
						);
					}
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "An error occurred while sending market alerts for user '{userId}'", user.Id);
				}
			}
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while executing the periodic task.");
            }

            try
            {
				var delaySecs = Random.Shared.Next(10*60, 20*60);
				Logger.LogInformation("Waiting for {delaySecs} seconds before the next execution...", delaySecs);
                await Task.Delay(delaySecs*1000, cancellationToken);
            }
            catch (TaskCanceledException) {}
        }
	}

	private async Task<IDictionary<string, StockQuote>> FetchQuotesForTickers(IEnumerable<string> tickers, IFinHubClient finHubClient, CancellationToken cancellationToken)
	{
		var clonedTickers = tickers.Select(YFUtils.BuildYFTicker).ToList();
		var quotesMap = new Dictionary<string, StockQuote>();
		while (clonedTickers.Count > 0)
		{
			var currentChunk = clonedTickers.Take(5).ToList();
			clonedTickers = [.. clonedTickers.Skip(5)];

			var tickersAsCommaSeparatedList = string.Join(",", currentChunk);
			try
			{
				var finhubQuotesResult = await finHubClient.GetStockQuotesAsync(tickersAsCommaSeparatedList, cancellationToken: cancellationToken);
				foreach (var quote in finhubQuotesResult.Data ?? new Dictionary<string, StockQuote>())
				{
					quotesMap[quote.Key] = quote.Value;
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to fetch quotes for tickers: {tickers}. Error: {errorMessage}", tickersAsCommaSeparatedList, ex.Message);
			}
		}
		return quotesMap;
	}

	private static int AttentionLevel(MarketEventEntity e, IDictionary<string, decimal> yieldsMap)
	{
		if (e.MarketId=="VN")
		{
			if (e.Metadata?.Amount >= 3000)
			{
				return 3;
			}
			else if (yieldsMap.TryGetValue(e.ItemCode, out var yield) && yield >= 0.04m)
			{
				return 2;
			}
		}
		else if (yieldsMap.TryGetValue(e.ItemCode, out var yield))
		{
			if (e.Metadata?.Amount >= 1.00m && yield >= 0.04m)
			{
				return 3;
			}
			if (e.Metadata?.Amount >= 5.00m && yield >= 0.02m)
			{
				return 2;
			}
			if (e.Metadata?.Amount >= 0.03m && yield >= 0.07m)
			{
				return 2;
			}
			if (e.Metadata?.Amount >= 0.03m && yield >= 0.03m)
			{
				return 1;
			}
		}
		return 0;
	}

	private async Task MarketAlertDividendEvents(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, CancellationToken cancellationToken)
	{
		var today = DateTimeOffset.UtcNow.StartOfDay();
		var startDateDiv = today.PrevWeekDay().PrevWeekDay();
		var endDateDiv = today.AddDays(6);
		var eventsDividend = (await portfolioRepo.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateDiv, endDateDiv,
			[MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION],
			cancellationToken: cancellationToken) ?? []).ToList();

		var quotesMap = await FetchQuotesForTickers(eventsDividend.Select(e => e.ItemCode).Distinct(), finHubClient, cancellationToken);
		var yieldsMap = eventsDividend.Select(e => e.ItemCode).Distinct().ToDictionary(symbol => symbol, symbol => {
			var e = eventsDividend.First(ev => ev.ItemCode == symbol);
			var ticker = YFUtils.BuildYFTicker(e.ItemCode);
			var result = quotesMap.TryGetValue(ticker, out var quote) && e.Metadata?.Amount > 0 && quote.MarketPrice > 0 ? e.Metadata.Amount/quote.MarketPrice : 0;
			return result ?? 0;
		});

		eventsDividend = [.. eventsDividend.OrderBy(e => e.EventTime).ThenByDescending(e => AttentionLevel(e, yieldsMap)).Where(e => AttentionLevel(e, yieldsMap) > 0)];
		var distinctMarkets = eventsDividend.Select(e => e.MarketId).Distinct().OrderBy(m => m).ToList();
		var messages = new List<string>();
		foreach (var market in distinctMarkets)
		{
			var message = $"<strong>💰 {market} - Dividends/distributions events:</strong>\n<blockquote>";
			foreach (var e in eventsDividend.Where(e => e.MarketId == market))
			{
				var ticker = YFUtils.BuildYFTicker(e.ItemCode);
				var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"\n📊 <code>{yieldsMap[e.ItemCode]:P2}</code> -💲<code>{quote.MarketPrice:F2}</code>" : "";
				message += $"<a href=\"{e.Metadata!.Link??""}\">{e.ItemCode}</a> - <code>{e.Metadata?.Amount??0:F2}</code> - 📅 <code>{e.EventTime:yyyy-MM-dd}</code>{quoteInfo}\n";
			}
			message += "</blockquote><preview disabled />";
			messages.Add(message);
		}

		foreach (var chatId in chatIDs)
		{
			foreach (var message in messages)
			try
			{
				await teleBot.SendHtml(chatId, message);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Failed to send market alert for chat ID {chatId}: {message}", chatId, message);
			}
		}
	}

	private async Task MarketAlertNewListings(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, CancellationToken cancellationToken)
	{
		var today = DateTimeOffset.UtcNow.StartOfDay();
		var startDateListing = today.AddDays(-21);
		var endDateListing = today.AddDays(14);
		var eventsListing = await portfolioRepo.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateListing, endDateListing,
			[MarketEventEntity.EVENT_LISTING],
			cancellationToken: cancellationToken);

		var quotesMap = await FetchQuotesForTickers(eventsListing.Select(e => e.ItemCode).Distinct(), finHubClient, cancellationToken);
		var message = "<strong>🆕 New listings:</strong>\n<blockquote>";
		foreach (var e in eventsListing)
		{
			var ticker = YFUtils.BuildYFTicker(e.ItemCode);
			var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"(current price: <code>{quote.MarketPrice:F2}</code>)" : "";
			message += $"<a href=\"{e.Metadata!.Link??""}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime:yyyy-MM-dd}</code> -💲<code>{e.Metadata?.Price??0:F2}</code> {quoteInfo}\n";
		}
		message += "</blockquote><preview disabled />";

		foreach (var chatId in chatIDs)
		try
		{
			await teleBot.SendHtml(chatId, message);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to send market alert for chat ID {chatId}: {message}", chatId, message);
		}
	}
}
