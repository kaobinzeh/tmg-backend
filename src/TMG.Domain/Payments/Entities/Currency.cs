using TMG.Domain.Common.Entities;

namespace TMG.Domain.Payments.Entities;

public sealed class Currency : Entity, IAggregateRoot
{
    private Currency()
    {
    }

    private Currency(string currencyCode, string currencyName, string? currencySymbol, bool isActive)
    {
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        CurrencyName = currencyName.Trim();
        CurrencySymbol = string.IsNullOrWhiteSpace(currencySymbol) ? null : currencySymbol.Trim();
        IsActive = isActive;
    }

    public string CurrencyCode { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public string? CurrencySymbol { get; private set; }
    public bool IsActive { get; private set; }

    public static Currency Create(
        string currencyCode,
        string currencyName,
        bool isActive,
        string? currencySymbol = null) =>
        new(currencyCode, currencyName, currencySymbol, isActive);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
