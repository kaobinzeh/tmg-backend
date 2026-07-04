namespace TMG.Contracts.Commands.Payments;

/// <summary>
/// Settles a confirmed in-app rent payment against its tenancy: records the ledger entry, advances the rent
/// cycle, and archives the receipt. Routed from <c>SuccessfulPaymentConfirmedHandler</c> for the
/// <c>RentPayment</c> intent.
/// </summary>
public sealed record RecordRentPaymentCommand(
    Guid PaymentTransactionId,
    string MerchantReference,
    Guid TenancyId,
    decimal Amount,
    Guid CurrencyId) : BaseCommand;
