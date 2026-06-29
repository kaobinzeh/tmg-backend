namespace TMG.Domain.Common.Authentication;

public sealed record TwoFactorOtp(string Code, DateTimeOffset ExpiresAtUtc);
