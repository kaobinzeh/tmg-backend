namespace TMG.Infrastructure.Payments.Credo;

internal sealed record CredoInitializeTransactionResponse(
    string AuthorizationUrl,
    string Reference,
    string CredoReference,
    string? Crn);
