namespace TMG.WebAPI.Features.Authentication.Registrations;

public sealed record SignUpRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid CountryId,
    string FirstName,
    string LastName,
    /// <summary>The stakeholder role the user is registering as. Valid values: landlord, manager, lawyer, tenant.</summary>
    string StakeholderTypeKey);
