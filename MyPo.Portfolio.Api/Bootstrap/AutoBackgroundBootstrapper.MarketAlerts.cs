using System.Text;
using MyPo.Libs.Tempus;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Models;
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
					if (!(user.Metadata?.MarketAlertViaTelegram??false)					// market alert is not enabled
						|| string.IsNullOrEmpty(user.Metadata?.GetTelegramBotApiKey())	// Telegram bot API key is not configured
						|| !(user.Metadata?.GetTelegramChatIDs()??[]).Any()				// No Telegram chat IDs are configured
					)
					{
						continue;
					}
					var now = DateTimeOffset.Now.ToTimeZoneSilently(user.Metadata?.MarketAlertTimezone??"");
					if (now == null																// invalid timezone
						|| !now.Value.WithinDowList(user.Metadata?.MarketAlertDaysOfWeek??[])	// not in the configured days to send alerts
						|| !now.Value.WithinTimeWindow(user.Metadata?.MarketAlertStartTime??TimeOnly.MinValue, user.Metadata?.MarketAlertEndTime??TimeOnly.MaxValue)
					)
					{
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
						// checkpoint is not ready for sending alerts yet
						continue;
					}

					var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
					var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
					var teleBot = new TelegramBotClient(user.Metadata!.GetTelegramBotApiKey()!);
					// await MarketAlertDividendEvents(portfolioRepo, finHubClient, teleBot, user.Metadata.GetTelegramChatIDs()??[], cancellationToken);
					await MarketAlertNewListings(portfolioRepo, finHubClient, teleBot, user.Metadata.GetTelegramChatIDs()??[], cancellationToken);

					checkpoint.CheckpointTime = DateTimeOffset.UtcNow;
					// var dbresult = await portfolioRepo.UpdateCheckpointAsync(checkpoint, cancellationToken);
					// if (dbresult == null)
					// {
					// 	Logger.LogError(
					// 		"Failed to update checkpoint: Owner: {owner} - Portfolio: {portfolio} - Market: {market} - Item: {item} - Type: {type}.",
					// 		checkpoint.OwnerId, checkpoint.PortfolioId, checkpoint.MarketId, checkpoint.ItemCode, checkpoint.CheckpointType
					// 	);
					// }
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

	private async Task MarketAlertDividendEvents(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, CancellationToken cancellationToken)
	{
		var today = DateTimeOffset.UtcNow.StartOfDay();
		var startDateDiv = today.PrevWeekDay().PrevWeekDay();
		var endDateDiv = today.AddDays(6);
		var eventsDividend = (await portfolioRepo.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateDiv, endDateDiv,
			[MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION],
			cancellationToken: cancellationToken)).ToList();
		if (eventsDividend.Count <= 0) return;

		var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(
			eventsDividend.Select(e => e.ItemCode).Distinct(),
			finHubClient, cancellationToken: cancellationToken);
		var yieldsMap = eventsDividend.Select(e => e.ItemCode).Distinct().ToDictionary(symbol => symbol, symbol => {
			var e = eventsDividend.First(ev => ev.ItemCode == symbol);
			var ticker = YFUtils.BuildYFTicker(e.ItemCode);
			var result = quotesMap.TryGetValue(ticker, out var quote) && e.Metadata?.Dividend?.Amount > 0 && quote.MarketPrice > 0 ? e.Metadata.Dividend.Amount/quote.MarketPrice : 0;
			return result ?? 0;
		});

		eventsDividend = [.. eventsDividend
			.OrderBy(e => e.EventTime).ThenByDescending(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap))
			.Where(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap) > 0)];
		var preExDivPrices = (await TickerUtils.FetchPreExDivPricesAsync(eventsDividend, finHubClient, cancellationToken)).ToDictionary();

		var distinctMarkets = eventsDividend.Select(e => e.MarketId).Distinct().OrderBy(m => m).ToList();
		var messages = new List<string>();
		foreach (var market in distinctMarkets)
		{
			var msg = new StringBuilder($"<strong>💰 {market} - Dividends/distributions events:</strong>\n<blockquote>");
			foreach (var e in eventsDividend.Where(e => e.MarketId == market))
			{
				var tz = MarketEventUtils.MarketToDefaultTimeZoneId(e.MarketId);
				var ticker = YFUtils.BuildYFTicker(e.ItemCode);
				var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"\n📊 <code>{yieldsMap[e.ItemCode]:P2}</code> -💲<code>{quote.MarketPrice:F2}</code>" : "";
				msg.Append($"<a href=\"{e.Metadata!.Link??""}\">{e.ItemCode}</a> - <code>{e.Metadata?.Dividend?.Amount??0:F2}</code> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):yyyy-MM-dd}</code>{quoteInfo}\n");
				if (preExDivPrices.TryGetValue(e.ItemCode, out var prePrice))
				{
					var delta = (quotesMap[ticker].MarketPrice+e.Metadata?.Dividend?.Amount??0 - prePrice) / prePrice - 1;
					msg.Append($"vs Pre-ex-div price: <code>{prePrice:F2} ({(delta>0?"+":"")}{delta:P2})</code>\n\n");
				}
			}
			msg.Append("</blockquote><preview disabled />");
			messages.Add(msg.ToString());
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
		var eventsListing = (await portfolioRepo.GetMarketEventsAsync(
			MarketEventEntity.NON_OWNER,
			startDateListing, endDateListing,
			[MarketEventEntity.EVENT_LISTING],
			cancellationToken: cancellationToken)).ToList();
		if (eventsListing.Count <= 0) return;

		var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(eventsListing.Select(e => e.ItemCode).Distinct(), finHubClient, cancellationToken: cancellationToken);
		var message = "<strong>🆕 New listings:</strong>\n<blockquote>";
		foreach (var e in eventsListing)
		{
			var tz = MarketEventUtils.MarketToDefaultTimeZoneId(e.MarketId);
			var ticker = YFUtils.BuildYFTicker(e.ItemCode);
			var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"(curr: <code>{quote.MarketPrice:F2}</code>)" : "";
			// message += $"<a href=\"{e.Metadata!.Link??""}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):yyyy-MM-dd}</code> -💲<code>{e.Metadata?.Price??0:F2}</code> {quoteInfo}\n";
			message += $"{e.ItemCode} - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):yyyy-MM-dd}</code> -💲<code>{e.Metadata?.Listing?.Price??0:F2}</code> {quoteInfo}\n";
		}
		// message += "</blockquote><preview disabled />";
		message += "</blockquote>";

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
