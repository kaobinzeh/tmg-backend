namespace TMG.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenCreateVirtualAccountRequest(
    string ExternalReference,
    string AccountName,
    decimal Amount);
