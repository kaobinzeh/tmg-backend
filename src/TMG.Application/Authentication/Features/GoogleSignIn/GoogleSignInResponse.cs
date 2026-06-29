namespace TMG.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType);
