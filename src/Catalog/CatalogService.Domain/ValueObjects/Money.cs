namespace CatalogService.Domain.ValueObjects;

public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public static Money Zero => new(0, "BRL");

    private Money() 
    {
        Currency = "BRL";
    }

    private Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        return new Money(amount, currency);
    }
}