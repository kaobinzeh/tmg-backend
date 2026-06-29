namespace TMG.Infrastructure.Payments.SafeHaven.Payloads;

public sealed record SafeHavenTokenRequest(
    string GrantType,
    string ClientId,
    string ClientAssertion,
    string ClientAssertionType);
