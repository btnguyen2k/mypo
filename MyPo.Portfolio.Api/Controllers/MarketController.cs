using Microsoft.AspNetCore.Authorization;
using MyPo.Shared.Api.Controller;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class MarketsController : ApiBaseController
{
}
