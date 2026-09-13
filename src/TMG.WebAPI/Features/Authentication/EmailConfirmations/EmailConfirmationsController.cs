using Asp.Versioning;
using TMG.Application.Authentication.Features.ResendSignUpOtp;
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
    ResendSignUpOtpHandler resendHandler,
    IValidator<SignUpOtpRequest> validator,
    IValidator<ResendSignUpOtpRequest> resendValidator,
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

    [HttpPost("resend")]
    [ProducesResponseType<ResendSignUpOtpResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResendSignUpOtpResponse>> Resend(
        [FromBody] ResendSignUpOtpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await resendValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        await resendHandler.HandleAsync(
            new ResendSignUpOtpCommand(request.Email, ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        // Every outcome returns the same response so the endpoint cannot be used to probe
        // which email addresses have accounts, or which of them are already verified.
        return Accepted((string?)null, new ResendSignUpOtpResponse(
            "If the account exists and is not yet verified, a new OTP will be sent shortly."));
    }
}
