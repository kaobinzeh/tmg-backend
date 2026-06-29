using Asp.Versioning;
using FluentValidation;
using TMG.Application.Properties.Features.SetUnitAvailability;
using TMG.Application.Properties.Features.UpdateUnitRent;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Properties;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Units.Route)]
public sealed class UnitsController(
    UpdateUnitRentHandler updateUnitRentHandler,
    SetUnitAvailabilityHandler setUnitAvailabilityHandler,
    IValidator<UpdateUnitRentRequest> updateUnitRentValidator,
    IValidator<SetUnitAvailabilityRequest> setUnitAvailabilityValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPut("{unitId:guid}/rent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRent(
        [FromRoute] Guid unitId,
        [FromBody] UpdateUnitRentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateUnitRentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await updateUnitRentHandler.HandleAsync(
            new UpdateUnitRentCommand(unitId, request.RentAmount, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UpdateUnitRentStatus.NotAuthenticated => Unauthorized(),
            UpdateUnitRentStatus.UnitNotFound => NotFound(),
            _ => NoContent()
        };
    }

    [HttpPut("{unitId:guid}/availability")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAvailability(
        [FromRoute] Guid unitId,
        [FromBody] SetUnitAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await setUnitAvailabilityValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await setUnitAvailabilityHandler.HandleAsync(
            new SetUnitAvailabilityCommand(unitId, request.Status, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            SetUnitAvailabilityStatus.NotAuthenticated => Unauthorized(),
            SetUnitAvailabilityStatus.UnitNotFound => NotFound(),
            _ => NoContent()
        };
    }
}
