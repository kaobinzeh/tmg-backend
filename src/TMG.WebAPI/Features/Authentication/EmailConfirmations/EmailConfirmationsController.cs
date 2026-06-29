using Asp.Versioning;
using TMG.Application.Authentication.Features.SignUpOtp;
using TMG.Domain.Common.Auditing;
using TMG.WebAPI.Infrastructure;
using TMG.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Authentication.EmailConfirmations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.EmailConfirmationPolicy)]
[Route(EndpointUrl.EmailConfirmations.Route)]
public sealed class EmailConfirmationsController(
    SignUpOtpHandler handler,
    IValidator<SignUpOtpRequest> validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpOtpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignUpOtpResponse>> Handle(
        [FromBody] SignUpOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignUpOtpCommand(request.Email, request.Otp, ActorContext.FromAnonymousActor(currentActor));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpOtpStatus.Success => Ok(new SignUpOtpResponse("OTP verified. You can now sign in.")),
            SignUpOtpStatus.AlreadyVerified => Ok(new SignUpOtpResponse("The account was already verified.")),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid OTP",
                detail: "The OTP is invalid, expired, or has already been consumed.")
        };
    }
}
