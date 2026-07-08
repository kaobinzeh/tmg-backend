namespace TMG.Application.Authentication;

/// <summary>
/// Sign-up happens on a shared frontend with no client selection, so new
/// stakeholders are attached to the client configured for this deployment.
/// </summary>
public sealed class ClientOnboardingOptions
{
    public const string SectionName = "Clients:Onboarding";

    public Guid? DefaultClientId { get; init; }
}
