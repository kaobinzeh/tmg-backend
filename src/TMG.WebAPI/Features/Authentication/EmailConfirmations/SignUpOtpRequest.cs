namespace TMG.WebAPI.Features.Authentication.EmailConfirmations;

public sealed record SignUpOtpRequest(string Email, string Otp);
