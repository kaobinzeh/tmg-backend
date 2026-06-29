namespace TMG.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenCreateSubAccountAutoSweepDetailsPayload(
    string AccountNumber,
    string Schedule);
