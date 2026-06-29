namespace TMG.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenInitiateVerificationRequest(
    string Type,
    int Number,
    int DebitAccountNumber);
