using Microsoft.AspNetCore.Components;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioPlanDetails;

public abstract class CBasePlanView : ComponentBase
{
    [Parameter]
    public PortfolioPlanResp Plan { get; set; } = default!;

    protected static string pnlCssClass(decimal pnl)
    {
        if (pnl > 0)
        {
            return "text-success";
        }
        if (pnl < 0)
        {
            return "text-danger";
        }
        return "text-muted";
    }

    protected static string pnlBorderCssClass(decimal pnl)
    {
        return pnlCssClass(pnl) switch
        {
            "text-success" => "border-start-success",
            "text-danger" => "border-start-danger",
            _ => "border-start-secondary",
        };
    }
}
