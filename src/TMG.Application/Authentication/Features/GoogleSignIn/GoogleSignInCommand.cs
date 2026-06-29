namespace TMG.Application.Authentication.Features.GoogleSignIn;

using TMG.Domain.Common.Auditing;

public sealed record GoogleSignInCommand(
    string IdToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
