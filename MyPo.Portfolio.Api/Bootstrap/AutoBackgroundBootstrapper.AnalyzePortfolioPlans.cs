using System.Text;
using System.Text.RegularExpressions;
using MyPo.Libs;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Identity;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Background task that periodically analyzes users' portfolio plans. Two distinct analyses are run
/// per plan, each gated by its own timestamp in <see cref="PortfolioPlanMetadata"/>:
/// <list type="bullet">
/// <item>normal analysis (<see cref="PortfolioPlanMetadata.AnalysisRefreshTimestsmp"/>) — result is
/// persisted only;</item>
/// <item>spotlight analysis (<see cref="PortfolioPlanMetadata.SpotlightRefreshTimestsmp"/>) — result is
/// persisted and pushed to the user via a Telegram alert (Markdown).</item>
/// </list>
/// The cadence is controlled per user by <see cref="PortfolioPlanPreferences.AutoAnalyzeDays"/>.
/// </summary>
sealed partial class BackgroundPortfolioTaskAnalyzePortfolioPlans : BackgroundPortfolioTask
{
    public BackgroundPortfolioTaskAnalyzePortfolioPlans(
            IServiceProvider serviceProvider, ILogger<BackgroundPortfolioTaskAnalyzePortfolioPlans> logger
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
                            await AnalyzePortfolioPlansForUser(scope, user, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "An error occurred while auto-analyzing portfolio plans for user '{userId}'", user.Id);
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

    private async Task AnalyzePortfolioPlansForUser(IServiceScope scope, MyPoUser user, CancellationToken cancellationToken)
    {
        var userName = user.UserName!.ToLower();
        var prefs = user.Metadata?.GetPortfolioPlanPreferences();
        if (user.Metadata is null || prefs is null || prefs.AutoAnalyzeDays <= 0)  // auto-analyze disabled
        {
            return;
        }

        Logger.LogInformation("Auto-analyzing portfolio plans for user {userName}...", userName);
        var analyzeDelay = TimeSpan.FromDays(prefs.AutoAnalyzeDays);
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();

         // Telegram is only used to push spotlight alerts; it's optional and the analyses still run without it.
        var botApiKey = user.Metadata.GetPortfolioPlanTelegramBotApiKey();
        var chatIDs = (user.Metadata.GetPortfolioPlanTelegramChatIDs() ?? []).ToList();
        var teleBot = prefs.ViaTelegram && !string.IsNullOrEmpty(botApiKey) && chatIDs.Count > 0
            ? new TelegramBotClient(botApiKey)
            : null;

        var plans = await portfolioRepo.GetPortfolioPlansByOwnerUserIdAsync(user.Id, cancellationToken);
        foreach (var plan in plans)
        {
            try
            {
                var changed = await AnalyzePortfolioPlan(portfolioRepo, finHubClient, teleBot, chatIDs, plan, analyzeDelay, cancellationToken);
                if (changed) break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to auto-analyze portfolio plan '{planId}: {planName}' for user '{userName}'.", plan.Id, plan.Name, userName);
            }
        }
    }

    /// <summary>
    /// Runs the two analyses (normal and spotlight) for a single plan, each only if its own refresh
    /// timestamp is older than <paramref name="analyzeDelay"/>, persists any new result, and pushes a
    /// Telegram alert (Markdown) for a refreshed spotlight analysis.
    /// </summary>
    private async Task<bool> AnalyzePortfolioPlan(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient? teleBot, IEnumerable<string> chatIDs, PortfolioPlanEntity plan, TimeSpan analyzeDelay, CancellationToken cancellationToken)
    {
        plan.Metadata ??= new PortfolioPlanMetadata();

        // skip analysis for plans without a description: the analysis is driven by the investor theme
        if (string.IsNullOrWhiteSpace(plan.Metadata.Description))
        {
            Logger.LogInformation("Skipping analysis for portfolio plan '{planId}: {planName}': no description provided.", plan.Id, plan.Name);
            return false;
        }

        // resolve the plan's market via the linked portfolio's default market (if any)
        var portfolio = !string.IsNullOrEmpty(plan.PortfolioId)
            ? await portfolioRepo.GetPortfolioByIdAsync(plan.PortfolioId, cancellationToken)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;
        var country = market?.Country ?? "US";
        var allocation = BuildAllocationReqs(plan);
        var changed = false;
        var nowUtc = DateTimeOffset.UtcNow;

        var thisChecksum = plan.Metadata.CalcChecksumAnalysis();
        var oldChecksum = plan.Metadata.LastChecksumAnalysis;
        var checksumChanged = !string.Equals(thisChecksum, oldChecksum, StringComparison.OrdinalIgnoreCase);

        // normal analysis: persisted only, no alert
        var lastAnalysis = plan.Metadata.AnalysisRefreshTimestamp;
        if (lastAnalysis <= 0 || checksumChanged || nowUtc - DateTimeOffset.FromUnixTimeSeconds(lastAnalysis) >= analyzeDelay)
        {
            if (lastAnalysis <= 0)
            {
                Logger.LogCritical("'{planId}: {planName}' never run analysis before", plan.Id, plan.Name);
            }
            if (checksumChanged)
            {
                Logger.LogCritical("'{planId}: {planName}' desc/holdings changed since last analysis (old-checksum: {oldChecksum} vs new-checksum: {newChecksum})", plan.Id, plan.Name, oldChecksum, thisChecksum);
            }
            if (nowUtc - DateTimeOffset.FromUnixTimeSeconds(lastAnalysis) >= analyzeDelay)
            {
                Logger.LogCritical("'{planId}: {planName}' last analysis is too old (last refresh: {lastRefresh})", plan.Id, plan.Name, DateTimeOffset.FromUnixTimeSeconds(lastAnalysis));
            }

            Logger.LogInformation("Running normal analysis for portfolio plan '{planId}: {planName}'...", plan.Id, plan.Name);
            var portfolioAnalysis = await RunNormalAnalysis(finHubClient, plan, country, allocation, cancellationToken);
            if (portfolioAnalysis is not null)
            {
                plan.Metadata.AnalysisRefreshTimestamp = nowUtc.ToUnixTimeSeconds();
                plan.Metadata.Analysis = portfolioAnalysis.Analysis;
                plan.Metadata.RebalancePlan = portfolioAnalysis.RebalancePlan;
                changed = true;
            }
        }
        else
        {
            Logger.LogInformation("Skipping normal analysis for portfolio plan '{planId}: {planName}' (last refresh: {lastRefresh}).", plan.Id, plan.Name, DateTimeOffset.FromUnixTimeSeconds(lastAnalysis));
        }

        // spotlight analysis: persisted and pushed to Telegram as a Markdown alert
        var lastSpotlight = plan.Metadata.SpotlightRefreshTimestamp;
        if (lastSpotlight <= 0 || checksumChanged || nowUtc - DateTimeOffset.FromUnixTimeSeconds(lastSpotlight) >= analyzeDelay)
        {
            if (lastAnalysis <= 0)
            {
                Logger.LogCritical("'{planId}: {planName}' never run spotlight before", plan.Id, plan.Name);
            }
            if (checksumChanged)
            {
                Logger.LogCritical("'{planId}: {planName}' desc/holdings changed since last analysis (old-checksum: {oldChecksum} vs new-checksum: {newChecksum})", plan.Id, plan.Name, oldChecksum, thisChecksum);
            }
            if (nowUtc - DateTimeOffset.FromUnixTimeSeconds(lastSpotlight) >= analyzeDelay)
            {
                Logger.LogCritical("'{planId}: {planName}' last spotlight is too old (last refresh: {lastRefresh})", plan.Id, plan.Name, DateTimeOffset.FromUnixTimeSeconds(lastSpotlight));
            }

            Logger.LogInformation("Running spotlight analysis for portfolio plan '{planId}: {planName}'...", plan.Id, plan.Name);
            var portfolioSpotlight = await RunSpotlightAnalysis(finHubClient, plan, country, allocation, cancellationToken);
            if (portfolioSpotlight is not null)
            {
                plan.Metadata.SpotlightRefreshTimestamp = nowUtc.ToUnixTimeSeconds();
                plan.Metadata.Spotlight = portfolioSpotlight.Analysis;
                changed = true;
                // fire-and-forget: don't block the analysis loop on Telegram delivery
                _ = Task.Run(()=>SendSpotlightAlert(teleBot, chatIDs, plan, portfolioSpotlight.Analysis, cancellationToken), cancellationToken);
            }
        }
        else
        {
            Logger.LogInformation("Skipping spotlight analysis for portfolio plan '{planId}: {planName}' (last refresh: {lastRefresh}).", plan.Id, plan.Name, DateTimeOffset.FromUnixTimeSeconds(lastSpotlight));
        }

        if (changed)
        {
            Logger.LogInformation("Saving updated portfolio plan '{planId}: {planName}' after analysis...", plan.Id, plan.Name);
            plan.Metadata.LastChecksumAnalysis = thisChecksum;
            var dbresult = await portfolioRepo.UpdatePortfolioPlanAsync(plan, cancellationToken);
            if (dbresult is null)
            {
                Logger.LogError("Failed to persist auto-analyzed portfolio plan '{planId}: {planName}'.", plan.Id, plan.Name);
            }
        }

        return changed;
    }

    /// <summary>
    /// Runs the normal portfolio analysis. Mirrors <c>FinHubController.AnalyzePortfolioPlan</c>: builds a
    /// fresh portfolio when there are no (or mostly empty) holdings, otherwise analyzes the existing one.
    /// Returns the analysis text, or <c>null</c> if the call failed or the LLM reported an error.
    /// </summary>
    private async Task<PortfolioAnalysis?> RunNormalAnalysis(IFinHubClient finHubClient, PortfolioPlanEntity plan, string country, List<HoldingTickerReq> allocation, CancellationToken cancellationToken)
    {
        var holdings = plan.Metadata?.HoldingTickers ?? [];
        var countEntries = holdings.Count;
        var countPositive = holdings.Count(ht => ht.Shares > 0);
        var buildNew = countEntries == 0 || (double)countPositive / countEntries <= 0.5;
        var resp = buildNew
            ? await finHubClient.BuildPortfolioAsync(new BuildPortfolioReq
                {
                    Country = country,
                    InvestorTheme = plan.Metadata?.Description,
                    CurrentAllocation = allocation,
                }, cancellationToken: cancellationToken)
            : await finHubClient.AnalyzePortfolioAsync(new AnalyzePortfolioReq
                {
                    Country = country,
                    InvestorTheme = plan.Metadata?.Description,
                    CurrentAllocation = allocation,
                    BuildRebalancePlan = plan.Type == PortfolioPlanEntity.PLAN_TYPE_ALLOCATION,
                }, cancellationToken: cancellationToken);
        if (!resp.IsSuccess || resp.Data is null || resp.Data.LLMError)
        {
            Logger.LogWarning("Failed to analyze portfolio plan '{planId}: {planName}': {message}", plan.Id, plan.Name, resp.Data?.LLMErrorMsg ?? resp.Message);
            return null;
        }
        return resp.Data;
    }

    /// <summary>
    /// Runs the spotlight portfolio analysis (immediate risks/actions). Returns the analysis text, or
    /// <c>null</c> if the call failed or the LLM reported an error.
    /// </summary>
    private async Task<PortfolioAnalysis?> RunSpotlightAnalysis(IFinHubClient finHubClient, PortfolioPlanEntity plan, string country, List<HoldingTickerReq> allocation, CancellationToken cancellationToken)
    {
        var resp = await finHubClient.SpotlightPortfolioAsync(new SpotLightPortfolioReq { Country = country, InvestorTheme = plan.Metadata?.Description, CurrentAllocation = allocation }, cancellationToken: cancellationToken);
        if (!resp.IsSuccess || resp.Data is null || resp.Data.LLMError)
        {
            Logger.LogWarning("Failed to spotlight portfolio plan '{planId}: {planName}': {message}", plan.Id, plan.Name, resp.Data?.LLMErrorMsg ?? resp.Message);
            return null;
        }
        return resp.Data;
    }

    /// <summary>
    /// Sends the spotlight analysis to the user's configured Telegram chats as a Markdown message.
    /// No-op when Telegram is not configured for the user. This is invoked fire-and-forget, so it never
    /// throws: all failures are caught and logged.
    /// </summary>
    private async Task SendSpotlightAlert(TelegramBotClient? teleBot, IEnumerable<string> chatIDs, PortfolioPlanEntity plan, string spotlight, CancellationToken cancellationToken)
    {
        if (teleBot is null)
        {
            return;
        }

        try
        {
            var message = BuildSpotlightMessage(plan, spotlight);
            foreach (var chatId in chatIDs)
            {
                try
                {
                    Logger.LogInformation("Sending spotlight alert for portfolio plan '{planId}: {planName}' to Telegram chat ID {chatId}...", plan.Id, plan.Name, chatId);
                    await teleBot.SendMessage(chatId, message, parseMode: ParseMode.Markdown,
                        linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to send portfolio plan spotlight alert to chat ID {chatId}: {message}", chatId, message.Excerpt(50));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send spotlight alert for portfolio plan '{planId}: {planName}'.", plan.Id, plan.Name);
        }
    }

    /// <summary>
    /// Builds the FinHub request allocation list from a plan's current holdings.
    /// </summary>
    private static List<HoldingTickerReq> BuildAllocationReqs(PortfolioPlanEntity plan)
        => [.. (plan.Metadata?.HoldingTickers ?? []).Select(ht => new HoldingTickerReq
        {
            Ticker = ht.Ticker,
            TargetAllocation = ht.TargetAllocation,
            NumShares = ht.Shares,
            AvgPrice = ht.AveragePrice,
            MarketPrice = ht.MarketPrice,
            Tags = ht.Tags,
        })];

    [GeneratedRegex(@"^[ \t]*#{1,6}[ \t]+(.+?)[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeadingRegex();

    /// <summary>
    /// Formats the Telegram spotlight alert message. The analysis body is Markdown (as returned by
    /// <see cref="IFinHubClient.SpotlightPortfolioAsync"/>) and is sent using Telegram's Markdown parse
    /// mode. Telegram supports only a subset of Markdown, so heading lines (e.g. <c># Heading</c>) are
    /// converted to bold (<c>*Heading*</c>).
    /// </summary>
    private static string BuildSpotlightMessage(PortfolioPlanEntity plan, string spotlight)
    {
        var body = MarkdownHeadingRegex().Replace(spotlight ?? string.Empty, "*$1*");
        var msg = new StringBuilder();
        msg.Append($"*📊 Portfolio plan '{plan.Name}' - spotlight:*\n\n");
        msg.Append(body);
        return msg.ToString();
    }
}
