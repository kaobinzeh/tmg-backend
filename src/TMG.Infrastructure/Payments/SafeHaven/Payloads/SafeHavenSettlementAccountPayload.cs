namespace TMG.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenSettlementAccountPayload(
    string BankCode,
    string AccountNumber);
