namespace TMG.Contracts.Events;

public sealed record UserSignInSuccessful(
    string IpAddress,
    string UserAgent) : BaseEvent;
