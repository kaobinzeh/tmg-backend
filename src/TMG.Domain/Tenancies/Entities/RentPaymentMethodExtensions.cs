namespace TMG.Domain.Tenancies.Entities;

public static class RentPaymentMethodExtensions
{
    /// <summary>The label shown on the receipt for a payment method.</summary>
    public static string ToDisplayName(this RentPaymentMethod method) => method switch
    {
        RentPaymentMethod.BankTransfer => "Bank transfer",
        RentPaymentMethod.Cash => "Cash",
        RentPaymentMethod.Cheque => "Cheque",
        RentPaymentMethod.Card => "Card",
        RentPaymentMethod.Online => "Online payment",
        _ => "Other"
    };
}
