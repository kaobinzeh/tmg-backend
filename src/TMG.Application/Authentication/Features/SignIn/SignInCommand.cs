namespace TMG.Application.Authentication.Features.SignIn;

using TMG.Domain.Common.Auditing;

public sealed record SignInCommand(
    string Email,
    string Password,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
