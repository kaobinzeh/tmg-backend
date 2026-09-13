namespace TMG.Application.Authentication.Features.ResendSignUpOtp;

public enum ResendSignUpOtpStatus
{
    Accepted,
    UserNotFound,
    AlreadyVerified
}

public sealed record ResendSignUpOtpResult(ResendSignUpOtpStatus Status);
