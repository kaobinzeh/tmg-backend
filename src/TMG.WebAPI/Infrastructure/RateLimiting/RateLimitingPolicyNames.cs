namespace TMG.WebAPI.Infrastructure.RateLimiting;

public static class RateLimitingPolicyNames
{
    public const string SignInPolicy = "sign-in-policy";
    public const string RefreshSessionPolicy = "refresh-session-policy";
    public const string SignUpPolicy = "sign-up-policy";
    public const string EmailConfirmationPolicy = "email-confirmation-policy";
    public const string PasswordResetPolicy = "password-reset-policy";
}
