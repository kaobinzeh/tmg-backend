using Asp.Versioning;
using TMG.Application.Payments.Features.InitiatePayment;
using TMG.Contracts.Payments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Payments.InitiatePayment;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Payments.Route)]
public sealed class PaymentsController(
    InitiatePaymentHandler handler,
    InitiatePaymentValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("initiate")]
    [ProducesResponseType<InitiatePaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InitiatePaymentResponse>> Handle(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        _ = Enum.TryParse<PaymentIntent>(request.PaymentIntent, true, out var paymentIntent);

        var result = await handler.HandleAsync(
            new InitiatePaymentCommand(
                request.Amount,
                request.CurrencyId,
                paymentIntent,
                request.PaymentProviderId,
                ActorContext.FromCurrentActor(currentActor),
                request.TenancyId),
            cancellationToken);

        return Ok(new InitiatePaymentResponse(
            result.MerchantReference,
            result.PaymentStatus.ToString(),
            result.PaymentProviderId,
            result.PaymentProviderName,
            result.ExpiresAtUtc,
            result.PaymentMethodType.ToString(),
            result.InstructionFields));
    }
}
