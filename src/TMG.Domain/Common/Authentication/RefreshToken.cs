namespace TMG.Domain.Common.Authentication;

public sealed record RefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
