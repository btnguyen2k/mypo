using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class PreferencesController : ApiBaseController
{
	private readonly IAuthenticator? Authenticator;
	private readonly IAuthenticatorAsync? AuthenticatorAsync;
	private readonly IIdentityRepository IdentityRepository;
	private readonly IdentityOptions IdentityOptions;

	public PreferencesController(
		IIdentityRepository identityRepository,
		IOptions<IdentityOptions> identityOptions,
		IAuthenticator? authenticator,
		IAuthenticatorAsync? authenticatorAsync)
	{
		ArgumentNullException.ThrowIfNull(identityRepository, nameof(identityRepository));
		ArgumentNullException.ThrowIfNull(identityOptions, nameof(identityOptions));
		if (authenticator == null && authenticatorAsync == null)
		{
			throw new ArgumentNullException("No authenticator defined.");
		}

		IdentityRepository = identityRepository;
		IdentityOptions = identityOptions.Value;
		Authenticator = authenticator;
		AuthenticatorAsync = authenticatorAsync;
	}

	private async ValueTask<(ActionResult?, MyPoUser)> VerifyAuthTokenAndCurrentUser()
	{
		var jwtToken = GetAuthToken();
		var tokenValidationResult = await ValidateAuthTokenAsync(Authenticator, AuthenticatorAsync, jwtToken);
		if (tokenValidationResult.Status != 200)
		{
			// the auth token should still be valid
			return (ResponseNoData(403, tokenValidationResult.Error), null!);
		}

		var currentUser = await GetCurrentUserAsync(IdentityOptions, IdentityRepository);
		if (currentUser == null)
		{
			// should not happen
			return (_respAuthenticationRequired, null!);
		}

		return (null, currentUser);
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
		currentUser.Metadata.MarketAlertViaTelegram = req.EnableMarketAlertsViaTelegrams;
		if (req.EnableMarketAlertsViaTelegrams)
		{
			// check if time zone is valid
			try
			{
				TimeZoneInfo.FindSystemTimeZoneById(req.MarketAlertTimezone);
				currentUser.Metadata.MarketAlertTimezone = req.MarketAlertTimezone;
			}
			catch (TimeZoneNotFoundException)
			{
				return ResponseNoData(400, $"Invalid time zone '{req.MarketAlertTimezone}'");
			}

			// check alert delay time
			currentUser.Metadata.MarketAlertDelayMinutes = Math.Max(req.MarketAlertDelayMinutes, 60);

			// chek start/end time
			req.MarketAlertStartTime ??= currentUser.Metadata.MarketAlertStartTime ?? new TimeOnly(0, 0);
			req.MarketAlertEndTime ??= currentUser.Metadata.MarketAlertEndTime ?? new TimeOnly(23, 59);
			if (req.MarketAlertStartTime >= req.MarketAlertEndTime)
			{
				return ResponseNoData(400, "Market alert start time must be before end time.");
			}
			currentUser.Metadata.MarketAlertStartTime = req.MarketAlertStartTime;
			currentUser.Metadata.MarketAlertEndTime = req.MarketAlertEndTime;

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
			currentUser.Metadata.MarketAlertDaysOfWeek = req.MarketAlertDaysOfWeek?.Where(validDaysOfWeek.Contains).ToList()
				?? currentUser.Metadata.MarketAlertDaysOfWeek ?? [];

			// Telegram Bot API key & chat IDs
			if (!string.IsNullOrEmpty(req.TelegramBotApiKey))
			{
				currentUser.Metadata.SetTelegramBotApiKey(req.TelegramBotApiKey);
			}
			if (!string.IsNullOrEmpty(req.TelegramChatIDs))
			{
				currentUser.Metadata.SetTelegramChatIDs(req.TelegramChatIDs);
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
