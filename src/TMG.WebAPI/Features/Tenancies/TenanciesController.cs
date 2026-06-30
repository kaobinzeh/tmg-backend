using Asp.Versioning;
using FluentValidation;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Application.Tenancies.Features.AllocateUnit;
using TMG.Application.Tenancies.Features.RejectTenancyInvitation;
using TMG.Application.Tenancies.Features.UploadTenancyDocument;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Tenancies;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Tenancies.Route)]
public sealed class TenanciesController(
    AllocateUnitHandler allocateUnitHandler,
    AcceptTenancyInvitationHandler acceptTenancyInvitationHandler,
    RejectTenancyInvitationHandler rejectTenancyInvitationHandler,
    UploadTenancyDocumentHandler uploadTenancyDocumentHandler,
    IValidator<AllocateUnitRequest> allocateUnitValidator,
    IValidator<AcceptTenancyInvitationRequest> acceptValidator,
    IValidator<RejectTenancyInvitationRequest> rejectValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("allocations")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<AllocateUnitResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AllocateUnitResponse>> AllocateUnit(
        [FromBody] AllocateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await allocateUnitValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await allocateUnitHandler.HandleAsync(
            new AllocateUnitCommand(
                request.UnitId,
                request.TenantEmail,
                request.TenantFirstName,
                request.TenantLastName,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            AllocateUnitStatus.NotAuthenticated => Unauthorized(),
            AllocateUnitStatus.UnitNotFound => NotFound(),
            AllocateUnitStatus.UnitNotAvailable => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Unit not available",
                detail: "The unit is not available for allocation."),
            AllocateUnitStatus.EmailRegisteredToAnotherClient => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email registered to another client",
                detail: "The tenant email is registered to a different client and cannot be allocated here."),
            AllocateUnitStatus.TenantTypeNotConfigured => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenant type not configured",
                detail: "The 'tenant' stakeholder type is not configured for this client."),
            _ => Created(EndpointUrl.Tenancies.AllocationsV1, new AllocateUnitResponse(result.TenancyId!.Value))
        };
    }

    [HttpPost("accept-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation(
        [FromBody] AcceptTenancyInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await acceptValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await acceptTenancyInvitationHandler.HandleAsync(
            new AcceptTenancyInvitationCommand(
                request.Email,
                request.Token,
                request.Password,
                request.ConfirmPassword,
                request.AcceptedTerms,
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            AcceptTenancyInvitationStatus.InvalidInvitation => NotFound(),
            AcceptTenancyInvitationStatus.TermsNotAccepted => BadRequest("The terms and conditions must be accepted."),
            AcceptTenancyInvitationStatus.InvalidToken => BadRequest("The invitation token is invalid or has expired."),
            AcceptTenancyInvitationStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            _ => NoContent()
        };
    }

    [HttpPost("reject-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectInvitation(
        [FromBody] RejectTenancyInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await rejectValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await rejectTenancyInvitationHandler.HandleAsync(
            new RejectTenancyInvitationCommand(
                request.Email,
                request.Token,
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            RejectTenancyInvitationStatus.InvalidInvitation => NotFound(),
            RejectTenancyInvitationStatus.InvalidToken => BadRequest("The invitation token is invalid or has expired."),
            _ => NoContent()
        };
    }

    [HttpPost("documents")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType<UploadTenancyDocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadTenancyDocumentResponse>> UploadDocument(
        [FromForm] UploadTenancyDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest("Document file is required.");
        }

        await using var fileStream = request.File.OpenReadStream();
        var result = await uploadTenancyDocumentHandler.HandleAsync(
            new UploadTenancyDocumentCommand(
                request.DocumentType,
                fileStream,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UploadTenancyDocumentStatus.NotAuthenticated => Unauthorized(),
            UploadTenancyDocumentStatus.NoActiveTenancy => NotFound(),
            UploadTenancyDocumentStatus.InvalidFile => BadRequest(result.Error ?? "Invalid document file."),
            _ => Created(EndpointUrl.Tenancies.DocumentsV1, new UploadTenancyDocumentResponse(result.DocumentId!.Value))
        };
    }
}
