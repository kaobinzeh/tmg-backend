using Asp.Versioning;
using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Stakeholders.Me;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route($"{EndpointUrl.Stakeholders.Route}/me")]
public sealed class MeController(
    GetMyProfileHandler getMyProfileHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<GetMyProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetMyProfileResponse>> GetMe(CancellationToken cancellationToken)
    {
        var result = await getMyProfileHandler.HandleAsync(
            new GetMyProfileCommand(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetMyProfileStatus.NotAuthenticated => Unauthorized(),
            GetMyProfileStatus.StakeholderNotFound => NotFound(),
            _ => Ok(result.Profile)
        };
    }
}
