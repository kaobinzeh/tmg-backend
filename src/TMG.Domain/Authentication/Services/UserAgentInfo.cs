namespace TMG.Domain.Authentication.Services;

public sealed record UserAgentInfo(
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName);
