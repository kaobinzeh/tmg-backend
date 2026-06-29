namespace TMG.Application.Authentication.Features.LogoutSession;

using TMG.Domain.Common.Auditing;

public sealed record LogoutSessionCommand(
    string TokenId,
    DateTimeOffset ExpiresAtUtc,
    Guid? StakeholderId,
    ActorContext ActorContext);
