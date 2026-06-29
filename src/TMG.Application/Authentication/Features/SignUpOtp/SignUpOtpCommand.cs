namespace TMG.Application.Authentication.Features.SignUpOtp;

using TMG.Domain.Common.Auditing;

public sealed record SignUpOtpCommand(string Email, string Otp, ActorContext ActorContext);
