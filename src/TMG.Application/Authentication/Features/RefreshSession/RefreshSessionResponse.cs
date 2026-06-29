namespace TMG.Application.Authentication.Features.RefreshSession;

public sealed record RefreshSessionResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType);
