namespace TMG.Application.Authentication.Features.RefreshSession;

using TMG.Domain.Common.Auditing;

public sealed record RefreshSessionCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
