using Microsoft.AspNetCore.Components;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioPlanDetails;

public abstract class CBasePlanView : ComponentBase
{
    [Parameter]
    public PortfolioPlanResp Plan { get; set; } = default!;
}
