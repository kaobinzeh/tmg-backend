namespace TMG.Contracts.Events;

public sealed record UserAccessTokenRefreshed(
    string IpAddress,
    string UserAgent) : BaseEvent;