using PDV.Core.Entities.Base;
using PDV.Shared.Enums;

namespace PDV.Core.Entities;

public class Payment : Entity
{
    public Guid SaleId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string? AuthorizationCode { get; private set; } // NSU
    public string? CardBrand { get; private set; }
    public DateTime PaymentDate { get; private set; }

    // Navigation
    public Sale? Sale { get; private set; }

    private Payment() { }

    public Payment(
        Guid saleId,
        PaymentMethod method,
        decimal amount,
        string? authorizationCode = null,
        string? cardBrand = null)
    {
        SaleId = saleId;
        Method = method;
        SetAmount(amount);
        AuthorizationCode = authorizationCode;
        CardBrand = cardBrand;
        PaymentDate = DateTime.UtcNow;
    }

    public void SetAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Amount = amount;
        SetUpdatedAt();
    }

    public void SetAuthorizationCode(string authorizationCode)
    {
        AuthorizationCode = authorizationCode;
        SetUpdatedAt();
    }
}
