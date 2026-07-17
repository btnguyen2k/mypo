using System.Text.Json;
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
    public async ValueTask<ActionResult<ApiResp<string[]>>> Debug()
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        // TODO
        var result = new List<string>();

        var portfolioId = "752b4a6c-e73d-45de-91f2-1c943c2a6062";
        var startPeriodIncl = new DateTime(2025, 12, 1);
        var endPeriodExcl = new DateTime(2026, 01, 01);
        var pnlSummary = await PortfolioRepository.GetPnlSummaryForPortfolioForPeriodAsync(portfolioId, startPeriodIncl, endPeriodExcl);
        result.Add($"Portfolio Id: {portfolioId}");
        result.Add($"Period: {startPeriodIncl:yyyy-MM-dd} to {endPeriodExcl:yyyy-MM-dd}");
        result.Add($"PnL Summary: {JsonSerializer.Serialize(pnlSummary)}");

        return ResponseOk("Debug endpoint is working.", result.ToArray());
    }
}
