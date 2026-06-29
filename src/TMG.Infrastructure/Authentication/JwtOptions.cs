namespace TMG.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TMG";
    public string Audience { get; init; } = "TMG.Clients";
    public string SigningKey { get; init; } = "super-secret-template-signing-key-change-me";
    public int LifetimeMinutes { get; init; } = 5;
}
