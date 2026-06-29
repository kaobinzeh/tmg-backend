namespace TMG.Application.Authentication.Features.GoogleSignUp;

using TMG.Domain.Common.Auditing;

public sealed record GoogleSignUpCommand(
    string IdToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    /// <summary>The stakeholder role key the user is signing up as (e.g. "landlord", "tenant").</summary>
    string StakeholderTypeKey,
    ActorContext ActorContext);
