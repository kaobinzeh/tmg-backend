namespace TMG.Domain.Stakeholders.ReadModels;

public sealed record StakeholderReadModel(
    Guid StakeholderId,
    Guid AppUserId,
    string EmailAddress,
    Guid ClientId,
    Guid CountryId,
    Guid StakeholderTypeId,
    string StakeholderTypeKey,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified);
