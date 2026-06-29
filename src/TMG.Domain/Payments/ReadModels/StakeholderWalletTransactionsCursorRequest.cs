namespace TMG.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionsCursorRequest(
    Guid StakeholderId,
    DateTimeOffset? CursorCreatedAtUtc,
    Guid? CursorTransactionId,
    int Limit);
