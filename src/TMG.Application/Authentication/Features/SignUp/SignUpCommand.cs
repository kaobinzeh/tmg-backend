namespace TMG.Application.Authentication.Features.SignUp;

using TMG.Domain.Common.Auditing;

public sealed record SignUpCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid CountryId,
    string FirstName,
    string LastName,
    /// <summary>The stakeholder role key the user is signing up as (e.g. "landlord", "tenant").</summary>
    string StakeholderTypeKey,
    ActorContext ActorContext);
