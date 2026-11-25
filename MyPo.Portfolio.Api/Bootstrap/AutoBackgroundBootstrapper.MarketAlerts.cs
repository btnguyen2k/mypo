using System.Net;
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
using FinHub.Client.Models.Listings;

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
            Logger.LogWarning("Skipping market alerts for user {userName}: user has not enabled market alerts or missing Telegram configuration.", userName);
            return;
        }
        var now = DateTimeOffset.Now.ToTimeZoneSilently(prefs.Timezone);
        if (now is null                                                         // invalid timezone
            || !now.Value.IsOnDayOfWeek(prefs.DaysOfWeek)                       // not in the configured days to send alerts
            || !now.Value.IsWithinTimeWindow(prefs.StartTime ?? TimeOnly.MinValue, prefs.EndTime ?? TimeOnly.MaxValue)
        )
        {
            Logger.LogWarning("Skipping market alerts for user {userName}: current time is outside the configured alert window.", userName);
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
            Logger.LogWarning("Skipping market alerts for user {userName}: alert delay has not elapsed.", userName);
            return;
        }

        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
        var teleBot = new TelegramBotClient(botApiKey);

        await MarketAlertNewListings(portfolioRepo, finHubClient, teleBot, chatIDs, cancellationToken);

        var userPlans = await portfolioRepo.GetPortfolioPlansByOwnerUserIdAsync(user.Id, cancellationToken);
        var tickers = userPlans
            .SelectMany(plan => plan.Metadata?.HoldingTickers ?? [])
            .Select(holding => holding.Ticker)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        await MarketAlertDividendEvents(portfolioRepo, finHubClient, teleBot, chatIDs, tickers, cancellationToken);

        await SaveCheckpoint(checkpoint, cancellationToken);
    }

    private async Task MarketAlertDividendEvents(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, IReadOnlySet<string> tickers, CancellationToken cancellationToken)
    {
        if (tickers.Count == 0) return;

        var today = DateTimeOffset.UtcNow.StartOfDay();
        var startDateDiv = today.PrevWeekDay().PrevWeekDay();
        var endDateDiv = today.AddDays(14);
        var eventsDividend = (await portfolioRepo.GetMarketEventsAsync(
            MarketEventEntity.NON_OWNER,
            startDateDiv, endDateDiv,
            [MarketEventEntity.EVENT_DIVIDEND, MarketEventEntity.EVENT_DISTRIBUTION],
            cancellationToken: cancellationToken))
            .Where(e => tickers.Contains(e.ItemCode))
            .ToList();
        if (eventsDividend.Count == 0) return;

        var symbolsDistincted = eventsDividend.Select(e => e.ItemCode).Distinct().ToList();
        var quotesMap = await TickerUtils.FetchQuotesForTickersAsync(symbolsDistincted, finHubClient, cancellationToken: cancellationToken);
        var yieldsMap = symbolsDistincted.ToDictionary(symbol => symbol, symbol =>
        {
            var e = eventsDividend.First(ev => ev.ItemCode == symbol);
            return e.Metadata?.Dividend?.DividendYield ?? 0;
        });

        // // only look at dividend events that we should pay attention to, sorted by event time and attention level
        // eventsDividend = [.. eventsDividend
        //     .OrderBy(e => e.EventTime).ThenByDescending(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap))
        //     .Where(e => MarketEventUtils.AttentionLevelForDividend(e, yieldsMap) > 0)];
        // // var preExDivPrices = (await TickerUtils.FetchPreExDivPricesAsync(eventsDividend, finHubClient, cancellationToken)).ToDictionary();

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
                msg.Append($"<a href=\"{e.Metadata!.Dividend?.Link ?? ""}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):MMM-dd}</code> -💲<code>{(e.Metadata?.Dividend?.Amount ?? 0).ToString(formatValue)} ({yieldsMap[e.ItemCode].ToString(formatPercent)}</code>)\n");
                if (e.Metadata?.Dividend?.Analysis != null)
                {
                    msg.Append($"📈 Recov: {e.Metadata.Dividend.Analysis.RecoveryProbability:P0} ({e.Metadata.Dividend.Analysis.RecoveryDaysMin}-{e.Metadata.Dividend.Analysis.RecoveryDaysMax} days)\n");
                    if (quotesMap.TryGetValue(ticker, out var quote))
                    {
                        msg.Append(quote.MarketPrice?.ToString(formatValue));
                    }
                    msg.Append(" → ");
                    if (quote?.MarketPrice is { } currentPrice)
                    {
                        try
                        {
                            var minimum = currentPrice - (decimal)e.Metadata.Dividend.Analysis.DropPriceMax;
                            var maximum = currentPrice - (decimal)e.Metadata.Dividend.Analysis.DropPriceMin;
                            msg.Append($"({minimum.ToString(formatValue)} - {maximum.ToString(formatValue)})");
                        }
                        catch (OverflowException)
                        {
                            msg.Append("(--)");
                        }
                    }
                    else
                    {
                        msg.Append("(--)");
                    }
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
        var startDateListing = today.AddDays(-14);
        var endDateListing = today.AddDays(21);
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
            var quoteInfo = quotesMap.TryGetValue(ticker, out var quote) ? $"(now: <code>{quote.MarketPrice:F2}</code>)" : "";
            var listing = e.Metadata?.Listing;
            if (listing is not null)
            {
                message.Append($"<a href=\"{listing.Link}\">{e.ItemCode}</a> - 📅 <code>{e.EventTime.ToTimeZoneSilently(tz):MMM-dd}</code> -💲<code>{listing.IssuePrice ?? 0:F2}</code> {quoteInfo}\n");
                if (listing.AnalysisStatus == ListingAnalysisStatus.Completed && listing.Analysis is { } analysis)
                {
                    var stance = analysis.OverallStance == ListingStance.InsufficientData ? "Insufficient data" : analysis.OverallStance.ToString();
                    message.Append($"<strong>Analysis: {stance}</strong> ({Math.Clamp(analysis.OverallConfidence, 0, 100)}% confidence, {analysis.OverallDataQuality} data quality)\n");

                    // DROP to make the alert message concise
                    // if (!string.IsNullOrWhiteSpace(analysis.ExecutiveSummary?.Summary))
                    // {
                    //     message.Append(WebUtility.HtmlEncode(analysis.ExecutiveSummary.Summary)).Append('\n');
                    // }

                    (string Label, ListingPeriodOutlook? Period)[] outlookPeriods =
                    [
                        ("IPO day", analysis.Outlook?.IpoDay),
                        ("First week", analysis.Outlook?.FirstWeek),
                        ("First two weeks", analysis.Outlook?.FirstTwoWeeks),
                        ("First month", analysis.Outlook?.FirstMonth),
                    ];
                    var priceFormat = e.MarketId == "VN" ? "F0" : "F2";
                    foreach (var (label, period) in outlookPeriods)
                    {
                        if (period is null) continue;

                        message.Append($"<strong>{label}:</strong> ");
                        if (period.AssessmentType == ListingOutlookAssessmentType.InsufficientData)
                        {
                            message.Append("Insufficient data");
                        }
                        else
                        {
                            var direction = period.Direction == ListingOutlookDirection.InsufficientData ? "Insufficient data" : period.Direction.ToString();
                            message.Append($"{period.AssessmentType} - {direction} ({Math.Clamp(period.Confidence, 0, 100)}% confidence)");
                            if (FormatListingAlertRange(period.ExpectedPriceMin, period.ExpectedPriceMax, value => value.ToString(priceFormat)) is { } priceRange)
                            {
                                message.Append($" | Price: <code>{priceRange}</code>");
                            }
                            if (FormatListingAlertRange(period.ExpectedReturnMinPct, period.ExpectedReturnMaxPct, value => $"{value:+0.0;-0.0;0.0}%") is { } returnRange)
                            {
                                message.Append($" | Return: <code>{returnRange}</code>");
                            }
                        }

                        // DROP to make the alert message concise
                        // if (!string.IsNullOrWhiteSpace(period.Rationale))
                        // {
                        //     message.Append(" - ").Append(WebUtility.HtmlEncode(period.Rationale));
                        // }
                        message.Append('\n');
                    }
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

    private static string? FormatListingAlertRange(double? minimum, double? maximum, Func<double, string> format)
    {
        var lower = minimum is { } min ? format(min) : null;
        var upper = maximum is { } max ? format(max) : null;
        if (lower is null)
        {
            return upper is null ? null : $"Up to {upper}";
        }
        return upper is null ? $"From {lower}" : $"{lower} to {upper}";
    }
}
