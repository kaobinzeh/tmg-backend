namespace TMG.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenInitiateVerificationPayload(
    string Type,
    int Number,
    int DebitAccountNumber,
    bool Async);
