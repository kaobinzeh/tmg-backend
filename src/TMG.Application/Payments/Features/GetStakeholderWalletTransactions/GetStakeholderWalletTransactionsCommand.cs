using TMG.Domain.Common.Auditing;

namespace TMG.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
