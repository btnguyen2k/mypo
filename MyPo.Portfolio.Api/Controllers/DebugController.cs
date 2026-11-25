using System.Text.Json;
using Finhub.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class DebugController : MyPoBaseController
{
    public DebugController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository
    ) : base(identityRepository, identityOptions, authenticator, authenticatorAsync, portfolioRepository)
    {
    }

    [HttpGet(IPortfolioApiClient.API_DEBUG)]
    public async ValueTask<ActionResult<ApiResp<string[]>>> Debug(IFinHubClient finHubClient)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var upcomingEarnings = await finHubClient.GetUpcomingEarningsAnnouncementsAsync("US");
        if ( upcomingEarnings is null || !upcomingEarnings.IsSuccess)
        {
            Console.WriteLine("Failed to fetch upcoming earnings.");
        }
        else
        {
            foreach (var item in upcomingEarnings.Data ?? [])
            {
                Console.WriteLine(JsonSerializer.Serialize(item));
            }
        }

        return ResponseOk("Debug endpoint is working.");
    }
}
