using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class PreferencesController : MyPoBaseController
{
    public PreferencesController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository
    ) : base(identityRepository, identityOptions, authenticator, authenticatorAsync, portfolioRepository)
    {
    }

    /// <summary>
    /// Saves the current user's preferences for market alerts.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpPost(IPortfolioApiClient.API_MY_PREFERENCES_MARKET_ALERT)]
    public async ValueTask<ActionResult<ApiResp>> SaveMyPreferencesMarketAlert([FromBody] SaveMyPrefMarketAlertReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        currentUser.Metadata ??= new MyPoUserMetadata();
        var prefs = new MarketAlertPreferences { ViaTelegram = req.EnableMarketAlertsViaTelegrams };
        if (req.EnableMarketAlertsViaTelegrams)
        {
            // check if time zone is valid
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(req.MarketAlertTimezone);
                prefs.Timezone = req.MarketAlertTimezone;
            }
            catch (TimeZoneNotFoundException)
            {
                return ResponseNoData(400, $"Invalid time zone '{req.MarketAlertTimezone}'");
            }

            // check alert delay time
            prefs.DelayMinutes = Math.Max(req.MarketAlertDelayMinutes, 60);

            // chek start/end time
            var existing = currentUser.Metadata.GetMarketAlertPreferences();
            req.MarketAlertStartTime ??= existing.StartTime ?? new TimeOnly(0, 0);
            req.MarketAlertEndTime ??= existing.EndTime ?? new TimeOnly(23, 59);
            if (req.MarketAlertStartTime >= req.MarketAlertEndTime)
            {
                return ResponseNoData(400, "Market alert start time must be before end time.");
            }
            prefs.StartTime = req.MarketAlertStartTime;
            prefs.EndTime = req.MarketAlertEndTime;

            // check days of week
            var validDaysOfWeek = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Monday", "Mon",
                "Tuesday", "Tue",
                "Wednesday", "Wed",
                "Thursday", "Thu",
                "Friday", "Fri",
                "Saturday", "Sat",
                "Sunday", "Sun"
            };
            prefs.DaysOfWeek = req.MarketAlertDaysOfWeek?.Where(validDaysOfWeek.Contains).ToList()
                ?? existing.DaysOfWeek;

            currentUser.Metadata.SetMarketAlertPreferences(prefs);

            // Telegram Bot API key & chat IDs (stored in the Market Alert group's own secret bag)
            var group = currentUser.Metadata.GetOrCreatePreferenceGroup(PreferenceGroupIds.MarketAlert);
            if (!string.IsNullOrEmpty(req.TelegramBotApiKey))
            {
                group.SetTelegramBotApiKey(req.TelegramBotApiKey);
            }
            if (!string.IsNullOrEmpty(req.TelegramChatIDs))
            {
                group.SetTelegramChatIDs(req.TelegramChatIDs);
            }
        }
        else
        {
            currentUser.Metadata.SetMarketAlertPreferences(prefs);
        }

        var dbresult = await IdentityRepository.UpdateAsync(currentUser);
        if (dbresult == null)
        {
            return ResponseNoData(500, "Error saving user info.");
        }
        return ResponseNoData(200, "Ok");
    }

    /// <summary>
    /// Saves the current user's preferences for portfolio plans.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpPost(IPortfolioApiClient.API_MY_PREFERENCES_PORTFOLIO_PLAN)]
    public async ValueTask<ActionResult<ApiResp>> SaveMyPreferencesPortfolioPlan([FromBody] SaveMyPrefPortfolioPlanReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        // basic validation
        if (req.PortfolioPlanAutoUpdateDays < 0 || req.PortfolioPlanAutoAnalyzeDays < 0)
        {
            return ResponseNoData(400, "Auto-update/analyze interval (in days) must be zero or a positive number.");
        }

        currentUser.Metadata ??= new MyPoUserMetadata();
        var prefs = new PortfolioPlanPreferences
        {
            ViaTelegram = req.EnablePortfolioPlanAlertsViaTelegrams,
            AutoUpdateDays = req.PortfolioPlanAutoUpdateDays,
            AutoAnalyzeDays = req.PortfolioPlanAutoAnalyzeDays,
        };
        currentUser.Metadata.SetPortfolioPlanPreferences(prefs);

        if (req.EnablePortfolioPlanAlertsViaTelegrams)
        {
            // Telegram Bot API key & chat IDs (stored in the Portfolio Plan group's own secret bag)
            var group = currentUser.Metadata.GetOrCreatePreferenceGroup(PreferenceGroupIds.PortfolioPlan);
            if (!string.IsNullOrEmpty(req.TelegramBotApiKey))
            {
                group.SetTelegramBotApiKey(req.TelegramBotApiKey);
            }
            if (!string.IsNullOrEmpty(req.TelegramChatIDs))
            {
                group.SetTelegramChatIDs(req.TelegramChatIDs);
            }
        }

        var dbresult = await IdentityRepository.UpdateAsync(currentUser);
        if (dbresult == null)
        {
            return ResponseNoData(500, "Error saving user info.");
        }
        return ResponseNoData(200, "Ok");
    }
}
