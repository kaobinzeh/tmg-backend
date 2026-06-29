namespace TMG.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpResult(SignUpOtpStatus Status);

public enum SignUpOtpStatus
{
    Success = 1,
    InvalidOtp = 2,
    AlreadyVerified = 3
}
