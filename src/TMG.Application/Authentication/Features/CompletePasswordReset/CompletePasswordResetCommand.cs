namespace TMG.Application.Authentication.Features.CompletePasswordReset;

using TMG.Domain.Common.Auditing;

public sealed record CompletePasswordResetCommand(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword,
    ActorContext ActorContext);
