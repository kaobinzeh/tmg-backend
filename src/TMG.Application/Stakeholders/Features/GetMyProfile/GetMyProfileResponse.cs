namespace TMG.Application.Stakeholders.Features.GetMyProfile;

public sealed record GetMyProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    string StakeholderTypeKey,
    Guid ClientId);
