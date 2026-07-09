using Asp.Versioning;
using FluentValidation;
using TMG.Application.Properties.Features.AddUnit;
using TMG.Application.Properties.Features.CreateProperty;
using TMG.Application.Properties.Features.GetProperty;
using TMG.Application.Properties.Features.ListProperties;
using TMG.Application.Properties.Features.ListUnits;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Properties;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireManager)]
[Route(EndpointUrl.Properties.Route)]
public sealed class PropertiesController(
    CreatePropertyHandler createPropertyHandler,
    ListPropertiesHandler listPropertiesHandler,
    GetPropertyHandler getPropertyHandler,
    AddUnitHandler addUnitHandler,
    ListUnitsHandler listUnitsHandler,
    IValidator<CreatePropertyRequest> createPropertyValidator,
    IValidator<AddUnitRequest> addUnitValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatePropertyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePropertyResponse>> CreateProperty(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createPropertyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await createPropertyHandler.HandleAsync(
            new CreatePropertyCommand(
                request.Name,
                request.Address,
                request.Description,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            CreatePropertyStatus.NotAuthenticated => Unauthorized(),
            _ => Created(
                EndpointUrl.Properties.V1,
                new CreatePropertyResponse(result.PropertyId!.Value))
        };
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PropertyListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PropertyListItem>>> ListProperties(CancellationToken cancellationToken)
    {
        var result = await listPropertiesHandler.HandleAsync(
            new ListPropertiesCommand(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            ListPropertiesStatus.NotAuthenticated => Unauthorized(),
            _ => Ok(result.Properties)
        };
    }

    [HttpGet("{propertyId:guid}")]
    [ProducesResponseType<PropertyDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDetail>> GetProperty(
        [FromRoute] Guid propertyId,
        CancellationToken cancellationToken)
    {
        var result = await getPropertyHandler.HandleAsync(
            new GetPropertyCommand(propertyId, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetPropertyStatus.NotAuthenticated => Unauthorized(),
            GetPropertyStatus.PropertyNotFound => NotFound(),
            _ => Ok(result.Property)
        };
    }

    [HttpPost("{propertyId:guid}/units")]
    [ProducesResponseType<AddUnitResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddUnitResponse>> AddUnit(
        [FromRoute] Guid propertyId,
        [FromBody] AddUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await addUnitValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await addUnitHandler.HandleAsync(
            new AddUnitCommand(
                propertyId,
                request.Label,
                request.Description,
                request.NumberOfRooms,
                request.RentAmount,
                request.CurrencyId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            AddUnitStatus.NotAuthenticated => Unauthorized(),
            AddUnitStatus.PropertyNotFound => NotFound(),
            AddUnitStatus.DuplicateLabel => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate unit label",
                detail: "A unit with this label already exists for the property."),
            _ => Created(
                EndpointUrl.Properties.UnitsV1(propertyId),
                new AddUnitResponse(result.UnitId!.Value))
        };
    }

    [HttpGet("{propertyId:guid}/units")]
    [ProducesResponseType<IReadOnlyList<UnitListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UnitListItem>>> ListUnits(
        [FromRoute] Guid propertyId,
        CancellationToken cancellationToken)
    {
        var result = await listUnitsHandler.HandleAsync(
            new ListUnitsCommand(propertyId, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            ListUnitsStatus.NotAuthenticated => Unauthorized(),
            ListUnitsStatus.PropertyNotFound => NotFound(),
            _ => Ok(result.Units)
        };
    }
}
