using System.Text;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Identity;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Background task that periodically analyzes users' portfolio plans and, when immediate actions are
/// recommended, sends alerts via Telegram. The cadence is controlled per user by
/// <see cref="PortfolioPlanPreferences.AutoAnalyzeDays"/>.
/// </summary>
/// <remarks>
/// SKELETON: the "immediate action" detection (<see cref="ShouldAlert"/>) and the alert message
/// formatting (<see cref="BuildAlertMessage"/>) are placeholders to be implemented later.
/// </remarks>
sealed class AutoBackgroundAnalyzePortfolioPlansScanner : AutoBackgroundAnnouncementScanner
{
    public AutoBackgroundAnalyzePortfolioPlansScanner(
            IServiceProvider serviceProvider, ILogger<AutoBackgroundAnalyzePortfolioPlansScanner> logger
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

            try
            {
                var delaySecs = Random.Shared.Next(10 * 60, 20 * 60);
                Logger.LogInformation("Waiting for {delaySecs} seconds before the next execution...", delaySecs);
                await Task.Delay(delaySecs * 1000, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }

    private async Task AnalyzePortfolioPlansForUser(IServiceScope scope, MyPoUser user, CancellationToken cancellationToken)
    {
        var userId = user.UserName!.ToLower();
        var prefs = user.Metadata?.GetPortfolioPlanPreferences();
        var botApiKey = user.Metadata?.GetPortfolioPlanTelegramBotApiKey();
        var chatIDs = (user.Metadata?.GetPortfolioPlanTelegramChatIDs() ?? []).ToList();
        if (user.Metadata is null || prefs is null || !prefs.ViaTelegram  // portfolio plan alerts not enabled
            || prefs.AutoAnalyzeDays <= 0                                 // auto-analyze disabled
            || string.IsNullOrEmpty(botApiKey)  // Telegram bot API key is not configured
            || chatIDs.Count == 0               // No Telegram chat IDs are configured
        )
        {
            return;
        }

        var checkpoint = await GetOrInitCheckpoint(
            ownerId: userId,
            portfolioId: CheckpointEntity.NON_PORTFOLIO,
            marketId: CheckpointEntity.NON_MARKET,
            itemCode: CheckpointEntity.NON_ITEM,
            checkpointType: CheckpointEntity.CHECKPOINT_PORTFOLIO_PLAN_ANALYZE,
            cancellationToken
        );
        var analyzeDelay = TimeSpan.FromDays(prefs.AutoAnalyzeDays);
        if (checkpoint is null || (checkpoint.CheckpointTime != DateTimeOffset.MinValue && DateTimeOffset.UtcNow - checkpoint.CheckpointTime < analyzeDelay))
        {
            // not due for an auto-analyze yet
            return;
        }

        Logger.LogInformation("Auto-analyzing portfolio plans for user {userId}...", userId);
        var portfolioRepo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var finHubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
        var teleBot = new TelegramBotClient(botApiKey);
        var plans = await portfolioRepo.GetPortfolioPlansByOwnerUserIdAsync(userId, cancellationToken);
        foreach (var plan in plans)
        {
            try
            {
                await AnalyzePortfolioPlanAndAlert(portfolioRepo, finHubClient, teleBot, chatIDs, plan, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to auto-analyze portfolio plan '{planId}' for user '{userId}'.", plan.Id, userId);
            }
        }

        await SaveCheckpoint(checkpoint, cancellationToken);
    }

    /// <summary>
    /// SKELETON: analyzes a single portfolio plan via the AI analysis API, persists the result, and
    /// sends a Telegram alert when immediate actions are recommended.
    /// </summary>
    private async Task AnalyzePortfolioPlanAndAlert(IPortfolioRepository portfolioRepo, IFinHubClient finHubClient, TelegramBotClient teleBot, IEnumerable<string> chatIDs, PortfolioPlanEntity plan, CancellationToken cancellationToken)
    {
        // resolve the plan's market via the linked portfolio's default market (if any)
        var portfolio = !string.IsNullOrEmpty(plan.PortfolioId)
            ? await portfolioRepo.GetPortfolioByIdAsync(plan.PortfolioId, cancellationToken)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;

        // call the AI analysis endpoint for the plan
        // TODO: mirror PortfolioController/FinHubController.AnalyzePortfolioPlan: choose between
        //       BuildPortfolioAsync and AnalyzePortfolioAsync based on the plan's holdings.
        var req = new AnalyzePortfolioReq
        {
            Country = market?.Country ?? "US",
            InvestorTheme = plan.Metadata?.Description,
            CurrentAllocation = [.. (plan.Metadata?.HoldingTickers ?? []).Select(ht => new HoldingTickerReq
            {
                Ticker = ht.Ticker,
                TargetAllocation = ht.TargetAllocation,
                NumShares = ht.Shares,
                AvgPrice = ht.AveragePrice,
                MarketPrice = ht.MarketPrice,
                Tags = ht.Tags,
            })],
        };
        var analysisResp = await finHubClient.AnalyzePortfolioAsync(req, cancellationToken: cancellationToken);
        if (!analysisResp.IsSuccess || analysisResp.Data is null || analysisResp.Data.LLMError)
        {
            Logger.LogWarning("Failed to analyze portfolio plan '{planId}': {message}", plan.Id, analysisResp.Message);
            return;
        }
        var analysis = analysisResp.Data;

        // persist the latest analysis result on the plan
        plan.Metadata ??= new PortfolioPlanMetadata();
        plan.Metadata.AnalysisRefreshTimestsmp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        plan.Metadata.Analysis = analysis.Analysis;
        await portfolioRepo.UpdatePortfolioPlanAsync(plan, cancellationToken);

        // only alert when the analysis flags an immediate action
        if (!ShouldAlert(analysis))
        {
            return;
        }
        var message = BuildAlertMessage(plan, analysis);
        foreach (var chatId in chatIDs)
        {
            try
            {
                await teleBot.SendHtml(chatId, message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send portfolio plan alert to chat ID {chatId}: {message}", chatId, message);
            }
        }
    }

    /// <summary>
    /// SKELETON placeholder: decides whether the analysis result warrants an immediate-action alert.
    /// </summary>
    /// <remarks>Returns <c>false</c> for now so the skeleton never sends alerts. TODO: implement the
    /// real "immediate action" detection (e.g. inspect <paramref name="analysis"/> for actionable signals).</remarks>
    private static bool ShouldAlert(PortfolioAnalysis analysis)
    {
        // TODO: implement immediate-action detection.
        return false;
    }

    /// <summary>
    /// SKELETON placeholder: formats the Telegram alert message for a plan's analysis result.
    /// </summary>
    private static string BuildAlertMessage(PortfolioPlanEntity plan, PortfolioAnalysis analysis)
    {
        // TODO: extract and format the actionable items from the analysis.
        var msg = new StringBuilder();
        msg.Append($"<strong>📊 Portfolio plan '{plan.Name}' - immediate actions:</strong>\n<blockquote>");
        msg.Append(analysis.Analysis);
        msg.Append("</blockquote><preview disabled />");
        return msg.ToString();
    }
}
