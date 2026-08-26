using System.Text;
using Ddth.Utilities.Tempus;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Shared.Identity;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Finhub.Client;

namespace MyPo.Portfolio.Api.Bootstrap;

sealed class BackgroundPortfolioTaskSendMarketAlerts : BackgroundPortfolioTask
{
    public BackgroundPortfolioTaskSendMarketAlerts(
            IServiceProvider serviceProvider, ILogger<BackgroundPortfolioTaskSendMarketAlerts> logger
        ) : base(serviceProvider, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // delay a bit to avoid all instances running at the same time after deployment or restart
        await DelayForRandomInterval(10, 30, "executing background job", cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                try
                {
                    var identityRepository = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
                    var allUsers = await identityRepository.GetAllUsersAsync(cancellationToken: cancellationToken);
                    foreach (var user in allUsers)
                    {
                        try
                        {
                            await SendMarketAlertsForUser(scope, user, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "An error occurred while sending market alerts for user '{userId}'", user.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred while executing the periodic task.");
                }
            }
            await DelayForRandomInterval(1 * 60 * 60, 2 * 60 * 60, cancellationToken: cancellationToken); // delay 1-2 hours before next execution
        }
    }

    private async Task SendMarketAlertsForUser(IServiceScope scope, MyPoUser user, CancellationToken cancellationToken)
    {
        var userName = user.UserName!.ToLower();
        Logger.LogInformation("Processing market alerts for user {userName}...", userName);
        var prefs = user.Metadata?.GetMarketAlertPreferences();
        var botApiKey = user.Metadata?.GetMarketAlertTelegramBotApiKey();
        var chatIDs = (user.Metadata?.GetMarketAlertTelegramChatIDs() ?? []).ToList();
        if (user.Metadata is null || prefs is null || !prefs.ViaTelegram  // market alert is not enabled
            || string.IsNullOrEmpty(botApiKey)  // Telegram bot API key is not configured
            || chatIDs.Count == 0               // No Telegram chat IDs are configured
        )
        {
            return;
        }
        var now = DateTimeOffset.Now.ToTimeZoneSilently(prefs.Timezone);
        if (now is null                                                         // invalid timezone
            || !now.Value.IsOnDayOfWeek(prefs.DaysOfWeek)                       // not in the configured days to send alerts
            || !now.Value.IsWithinTimeWindow(prefs.StartTime ?? TimeOnly.MinValue, prefs.EndTime ?? TimeOnly.MaxValue)
        )
        {
            return;
        }
        var checkpoint = await GetOrInitCheckpoint(
            ownerId: userName,
            portfolioId: CheckpointEntity.NON_PORTFOLIO,
            marketId: CheckpointEntity.NON_MARKET,
            itemCode: CheckpointEntity.NON_ITEM,
            checkpointType: CheckpointEntity.CHECKPOINT_MARKET_ALERTS,
            cancellationToken
        );
        var alertDelay = TimeSpan.FromMinutes(prefs.DelayMinutes);
        if (checkpoint is null || (checkpoint.CheckpointTime != DateTimeOffset.MinValue && DateTimeOffset.UtcNow - checkpoint.CheckpointTime < alertDelay))
        {
            // not due for sending alerts
            return;
        }

        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
        var teleBot = new TelegramBotClient(botApiKey);
        await MarketAlertDividendEvents(portfolioRepo, finHubClient, teleBot, chatIDs, cancellationToken);
        await MarketAlertNewListings(portfolioRepo, finHubClient, teleBot, chatIDs, cancellationToken);

        await SaveCheckpoint(checkpoint, cancellationToken);
    }

    private async Task MarketAlertDividendEvents(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.StartOfDay();
        var startDateDiv = today.PrevWeekDay().PrevWeekDay();
        var endDateDiv = today.AddDays(14);
        var eventsDividend = (await portfolioRepo.GetMarketEventsAsync(
            MarketEventEntity.NON_OWNER,
            startDateDiv, endDateDiv,
            [MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION],
            cancellationToken: cancellationToken)).ToList();
        if (eventsDividend.Count <= 0) return;

        var symbolsDistincted = eventsDividend.Select(e => e.ItemCode).Distinct().ToList();
        var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(symbolsDistincted, finHubClient, cancellationToken: cancellationToken);
        var yieldsMap = symbolsDistincted.ToDictionary(symbol => symbol, symbol =>
        {
            var e = eventsDividend.First(ev => ev.ItemCode == symbol);
            return e.Metadata?.Dividend?.DividendYield ?? 0;
        });

        // only look at dividend events that we should pay attention to, sorted by event time and attention level
        eventsDividend = [.. eventsDividend
            .OrderBy(e => e.EventTime).ThenByDescending(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap))
            .Where(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap) > 0)];
        // var preExDivPrices = (await TickerUtils.FetchPreExDivPricesAsync(eventsDividend, finHubClient, cancellationToken)).ToDictionary();

        var markets = eventsDividend.Select(e => e.MarketId).Distinct().OrderBy(m => m).ToList();
        var messages = new List<string>();
        var msg = new StringBuilder();
        foreach (var market in markets)
        {
            msg.Clear();
            msg.Append($"<strong>💰 {market} - Dividends/distributions events:</strong>\n<blockquote>");
            foreach (var e in eventsDividend.Where(e => e.MarketId == market))
            {
                var tz = MarketEventUtils.MarketToDefaultTimeZoneId(e.MarketId);
                var ticker = YFUtils.BuildYFTicker(e.ItemCode);
                var formatValue = market == "VN" ? "F0" : "F2";
                var formatPercent = market == "VN" ? "P0" : "P2";
                msg.Append($"<a href=\"{e.Metadata!.Link ?? ""}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):MMM-dd}</code> -💲<code>{(e.Metadata?.Dividend?.Amount ?? 0).ToString(formatValue)} ({yieldsMap[e.ItemCode].ToString(formatPercent)}</code>)\n");
                if (e.Metadata?.Dividend?.Analysis != null)
                {
                    msg.Append($"📈 Recov: {e.Metadata.Dividend.Analysis.RecoveryProb:P0} ({e.Metadata.Dividend.Analysis.RecoveryDaysMin}-{e.Metadata.Dividend.Analysis.RecoveryDaysMax} days)\n");
                    if (quotesMap.TryGetValue(ticker, out var quote))
                    {
                        msg.Append(quote.MarketPrice.ToString(formatValue));
                    }
                    msg.Append(" → ");
                    msg.Append($"({e.Metadata.Dividend.Analysis.DropPriceMin.ToString(formatValue)} - {e.Metadata.Dividend.Analysis.DropPriceMax.ToString(formatValue)})");
                    msg.Append(" → ");
                    msg.Append($"({e.Metadata.Dividend.Analysis.RecoveryPriceMin.ToString(formatValue)} - {e.Metadata.Dividend.Analysis.RecoveryPriceMax.ToString(formatValue)})\n");
                    msg.Append('\n');
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

        var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(
            eventsListing.Select(e => e.ItemCode).Distinct(),
            finHubClient, cancellationToken: cancellationToken);
        var message = new StringBuilder("<strong>🆕 New listings:</strong>\n<blockquote>");
        foreach (var e in eventsListing)
        {
            var tz = MarketEventUtils.MarketToDefaultTimeZoneId(e.MarketId);
            var ticker = YFUtils.BuildYFTicker(e.ItemCode);
            var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"(curr: <code>{quote.MarketPrice:F2}</code>)" : "";
            message.Append($"<a href=\"{e.Metadata?.Link}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):MMM-dd}</code> -💲<code>{e.Metadata?.Listing?.Price ?? 0:F2}</code> {quoteInfo}\n");
            if (e.Metadata?.Listing?.Analysis?.Outlook != null)
            {
                if (e.Metadata.Listing.Analysis.Outlook.TryGetValue("w2", out var v21))
                {
                    message.Append($"📈 2w: {v21.Direction} ({v21.Confidence}%), {v21.Reason}\n");
                }
                if (e.Metadata.Listing.Analysis.Outlook.TryGetValue("m1", out var m11))
                {
                    message.Append($"📈 1m: {m11.Direction} ({m11.Confidence}%), {m11.Reason}\n");
                }
                if (e.Metadata.Listing.Analysis.Outlook.TryGetValue("m3", out var m31))
                {
                    message.Append($"📈 3m: {m31.Direction} ({m31.Confidence}%), {m31.Reason}\n");
                }
            }
            message.Append('\n');
        }
        message.Append("</blockquote><preview disabled />");
        // message.Append("</blockquote>");

        foreach (var chatId in chatIDs)
            try
            {
                await teleBot.SendHtml(chatId, message.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send market alert for chat ID {chatId}: {message}", chatId, message.ToString());
            }
    }
}
