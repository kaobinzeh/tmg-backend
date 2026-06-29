using Asp.Versioning;
using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.SignUp;
using TMG.Domain.Common.Auditing;
using TMG.WebAPI.Infrastructure;
using TMG.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Authentication.Registrations;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting(RateLimitingPolicyNames.SignUpPolicy)]
[Route(EndpointUrl.Registrations.Route)]
public sealed class RegistrationsController(
    SignUpHandler handler,
    GoogleSignUpHandler googleSignUpHandler,
    IValidator<SignUpRequest> validator,
    IValidator<GoogleSignUpRequest> googleSignUpValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SignUpResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignUpResponse>> Handle(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var command = new SignUpCommand(
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.CountryId,
            request.FirstName,
            request.LastName,
            request.StakeholderTypeKey,
            ActorContext.FromAnonymousActor(currentActor));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Status switch
        {
            SignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            SignUpStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            _ => Accepted((string?)null, new SignUpResponse(
                request.Email,
                "The sign-up request has been accepted. The account verification OTP will be sent shortly."))
        };
    }

    [HttpPost("google")]
    [ProducesResponseType<GoogleSignUpResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GoogleSignUpResponse>> HandleGoogle(
        [FromBody] GoogleSignUpRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await googleSignUpValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await googleSignUpHandler.HandleAsync(
            new GoogleSignUpCommand(
                request.IdToken,
                request.CountryId,
                request.FirstName,
                request.LastName,
                request.StakeholderTypeKey,
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GoogleSignUpStatus.InvalidGoogleToken => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid Google token",
                detail: "The supplied Google identity token is invalid or expired."),
            GoogleSignUpStatus.DuplicateEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already exists",
                detail: "An account with this email address already exists."),
            GoogleSignUpStatus.DuplicateGoogleAccount => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Google account already linked",
                detail: "This Google account is already linked to another user."),
            GoogleSignUpStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(result.ValidationErrors ?? new Dictionary<string, string[]>()))),
            _ => Accepted((string?)null, new GoogleSignUpResponse(
                result.Email ?? string.Empty,
                "The Google sign-up request has been accepted and the email has been confirmed automatically."))
        };
    }
}
