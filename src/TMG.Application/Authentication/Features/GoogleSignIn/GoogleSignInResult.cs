using TMG.Domain.Common.Authentication;

namespace TMG.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInResult(
    GoogleSignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null);

public enum GoogleSignInStatus
{
    Success = 1,
    InvalidGoogleToken = 2,
    AccountNotRegistered = 3,
    EmailNotVerified = 4,
    AccountLocked = 5
}
