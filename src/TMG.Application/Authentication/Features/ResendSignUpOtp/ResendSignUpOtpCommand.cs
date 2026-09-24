using TMG.Domain.Common.Auditing;

namespace TMG.Application.Authentication.Features.ResendSignUpOtp;

public sealed record ResendSignUpOtpCommand(string Email, ActorContext ActorContext);
