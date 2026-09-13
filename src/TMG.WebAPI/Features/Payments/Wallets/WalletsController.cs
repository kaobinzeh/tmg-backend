using Asp.Versioning;
using TMG.Application.Payments.Features.GetStakeholderWalletBalances;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Payments.Wallets;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Payments.Route)]
public sealed class WalletsController(
    GetStakeholderWalletBalancesHandler handler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("wallets")]
    [ProducesResponseType<GetStakeholderWalletBalancesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetStakeholderWalletBalancesResult>> Handle(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetStakeholderWalletBalancesCommand(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return Ok(result);
    }
}
