namespace TMG.WebAPI.Features.Authentication.PasswordResets;

public sealed record CompletePasswordResetRequest(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword);
