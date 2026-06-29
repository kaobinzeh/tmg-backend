namespace TMG.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementRequest(
    string AccountId,
    int Page,
    int Limit,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Type);
