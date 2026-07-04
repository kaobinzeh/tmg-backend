using Asp.Versioning;
using FluentValidation;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Application.Tenancies.Features.ActivateTenancy;
using TMG.Application.Tenancies.Features.AllocateUnit;
using TMG.Application.Tenancies.Features.GetTenancyCycle;
using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Application.Tenancies.Features.RecordRentPayment;
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
    ActivateTenancyHandler activateTenancyHandler,
    RecordRentPaymentHandler recordRentPaymentHandler,
    GetTenancyCycleHandler getTenancyCycleHandler,
    ListUpcomingRenewalsHandler listUpcomingRenewalsHandler,
    IValidator<AllocateUnitRequest> allocateUnitValidator,
    IValidator<AcceptTenancyInvitationRequest> acceptValidator,
    IValidator<RejectTenancyInvitationRequest> rejectValidator,
    IValidator<RecordRentPaymentRequest> recordRentPaymentValidator,
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
                ActorContext.FromCurrentActor(currentActor),
                request.LeaseStartDate,
                request.TermMonths),
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

    [HttpPost("allocations/{tenancyId:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateTenancy(
        Guid tenancyId,
        [FromBody] ActivateTenancyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await activateTenancyHandler.HandleAsync(
            new ActivateTenancyCommand(
                tenancyId,
                ActorContext.FromCurrentActor(currentActor),
                request.LeaseStartDate,
                request.TermMonths),
            cancellationToken);

        return result.Status switch
        {
            ActivateTenancyStatus.NotAuthenticated => Unauthorized(),
            ActivateTenancyStatus.TenancyNotFound => NotFound(),
            ActivateTenancyStatus.MissingLeaseTerms => BadRequest(
                "A lease start date and term (months) are required to activate the tenancy."),
            ActivateTenancyStatus.NotAccepted => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenancy not accepted",
                detail: "The tenancy must be accepted by the tenant before its rent cycle can be activated."),
            _ => NoContent()
        };
    }

    [HttpPost("allocations/{tenancyId:guid}/payments")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<RecordRentPaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecordRentPaymentResponse>> RecordRentPayment(
        Guid tenancyId,
        [FromBody] RecordRentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await recordRentPaymentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await recordRentPaymentHandler.HandleAsync(
            new RecordRentPaymentCommand(
                tenancyId,
                request.Amount,
                request.Method,
                ActorContext.FromCurrentActor(currentActor),
                request.Reference,
                request.PaidAtUtc),
            cancellationToken);

        return result.Status switch
        {
            RecordRentPaymentStatus.NotAuthenticated => Unauthorized(),
            RecordRentPaymentStatus.TenancyNotFound => NotFound(),
            RecordRentPaymentStatus.InvalidAmount => BadRequest("The rent payment amount must be greater than zero."),
            RecordRentPaymentStatus.NotActive => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenancy not active",
                detail: "The tenancy must have an active rent cycle before a payment can be recorded."),
            _ => Created(
                EndpointUrl.Tenancies.PaymentsV1(tenancyId),
                new RecordRentPaymentResponse(result.RentPaymentId!.Value, result.ReceiptDocumentId))
        };
    }

    [HttpGet("allocations/{tenancyId:guid}/cycle")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<TenancyCycleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenancyCycleDto>> GetCycle(
        Guid tenancyId,
        CancellationToken cancellationToken)
    {
        var result = await getTenancyCycleHandler.HandleAsync(
            new GetTenancyCycleCommand(tenancyId, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetTenancyCycleStatus.NotAuthenticated => Unauthorized(),
            GetTenancyCycleStatus.TenancyNotFound => NotFound(),
            _ => Ok(result.Cycle)
        };
    }

    [HttpGet("allocations/upcoming-renewals")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<IReadOnlyList<UpcomingRenewalListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UpcomingRenewalListItem>>> ListUpcomingRenewals(
        [FromQuery] int withinMonths,
        CancellationToken cancellationToken)
    {
        var result = await listUpcomingRenewalsHandler.HandleAsync(
            new ListUpcomingRenewalsCommand(
                ActorContext.FromCurrentActor(currentActor),
                withinMonths <= 0 ? 6 : withinMonths),
            cancellationToken);

        return result.Status switch
        {
            ListUpcomingRenewalsStatus.NotAuthenticated => Unauthorized(),
            _ => Ok(result.Renewals)
        };
    }
}
