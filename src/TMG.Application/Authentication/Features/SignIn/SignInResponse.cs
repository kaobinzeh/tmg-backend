namespace TMG.Application.Authentication.Features.SignIn;

public sealed record SignInResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType);
