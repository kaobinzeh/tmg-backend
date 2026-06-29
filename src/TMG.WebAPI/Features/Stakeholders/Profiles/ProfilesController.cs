using Asp.Versioning;
using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Stakeholders.Profiles;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route($"{EndpointUrl.Stakeholders.Route}/me/profile")]
public sealed class ProfilesController(
    UploadAvatarHandler uploadAvatarHandler,
    UpdateProfileHandler updateProfileHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateProfileHandler.HandleAsync(
            new UpdateProfileCommand(request.FirstName, request.LastName, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UpdateProfileStatus.NotAuthenticated => Unauthorized(),
            UpdateProfileStatus.StakeholderNotFound => NotFound(),
            UpdateProfileStatus.ValidationFailed => BadRequest(result.Error ?? "Invalid profile payload."),
            _ => NoContent()
        };
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [ProducesResponseType<UploadAvatarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadAvatarResponse>> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Avatar is null)
        {
            return BadRequest("Avatar file is required.");
        }

        await using var avatarStream = request.Avatar.OpenReadStream();
        var result = await uploadAvatarHandler.HandleAsync(
            new UploadAvatarCommand(
                avatarStream,
                request.Avatar.FileName,
                request.Avatar.ContentType,
                request.Avatar.Length,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UploadAvatarStatus.NotAuthenticated => Unauthorized(),
            UploadAvatarStatus.StakeholderNotFound => NotFound(),
            UploadAvatarStatus.InvalidFile => BadRequest(result.Error ?? "Invalid avatar file."),
            _ => Ok(new UploadAvatarResponse(result.AvatarUrl ?? string.Empty))
        };
    }
}
