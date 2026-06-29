using TMG.Domain.Common.Auditing;

namespace TMG.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed record GetStakeholderWalletTopUpTransactionDetailCommand(
    Guid WalletTransactionId,
    ActorContext ActorContext);