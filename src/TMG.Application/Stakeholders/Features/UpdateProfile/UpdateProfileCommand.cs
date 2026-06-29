namespace TMG.Application.Stakeholders.Features.UpdateProfile;

using TMG.Domain.Common.Auditing;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    ActorContext ActorContext);
