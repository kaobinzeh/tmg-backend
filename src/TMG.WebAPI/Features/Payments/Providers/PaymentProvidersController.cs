using Asp.Versioning;
using TMG.Application.Payments.Features.ActivatePaymentProvider;
using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Contracts.Payments;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Payments.Providers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.PaymentProviders.Route)]
public sealed class PaymentProvidersController(
    ActivatePaymentProviderHandler handler,
    GetPaymentProvidersHandler getPaymentProvidersHandler,
    GetPaymentProvidersValidator getPaymentProvidersValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PaymentProviderListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PaymentProviderListItem>>> List(
        [FromQuery] GetPaymentProvidersRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await getPaymentProvidersValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var intent = string.IsNullOrWhiteSpace(request.Intent)
            ? PaymentIntent.RentPayment
            : Enum.Parse<PaymentIntent>(request.Intent, true);

        var result = await getPaymentProvidersHandler.HandleAsync(
            new GetPaymentProvidersCommand(intent),
            cancellationToken);

        return Ok(result.Providers);
    }

    [HttpPut("{id:guid}/activation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActivation(
        [FromRoute] Guid id,
        [FromBody] SetPaymentProviderActivationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ActivatePaymentProviderCommand(id, request.IsActive),
            cancellationToken);

        return result.Status switch
        {
            ActivatePaymentProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Payment provider not found."),
            _ => NoContent()
        };
    }
}
