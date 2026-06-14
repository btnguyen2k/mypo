using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
    /// <summary>
    /// Gets markets metadata.
    /// </summary>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MARKETS)]
    public ActionResult<ApiResp<List<MarketDefResp>>> GetMarkets()
    {
        var result = new List<MarketDefResp>();
        foreach (var market in Globals.Markets)
        {
            result.Add(MarketDefResp.BuildFrom(market));
        }
        return ResponseOk(result);
    }
}
