using TMG.Domain.Common.Authentication;

namespace TMG.Application.Authentication.Features.SignIn;

public sealed record SignInResult(
    SignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null);

public enum SignInStatus
{
    Success = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3,
    AccountLocked = 4
}
