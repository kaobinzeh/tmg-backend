namespace TMG.Domain.Authentication.Services;

public sealed record IpGeolocation(
    string? City,
    string? State,
    string? Country);
