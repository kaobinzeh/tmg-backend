namespace TMG.Infrastructure.Authentication;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Authentication:RefreshTokens";

    public int LifetimeDays { get; init; } = 30;
}
