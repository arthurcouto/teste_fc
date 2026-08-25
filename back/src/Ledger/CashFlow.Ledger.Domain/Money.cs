namespace CashFlow.Ledger.Domain;

public sealed record Money
{
    public const int Scale = 2;

    public const decimal MaxAmount = 99_999_999_999_999_999m;

    private Money(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public static Money Of(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidMoneyException("Entry amount must be greater than zero.");
        }

        if (amount > MaxAmount)
        {
            throw new InvalidMoneyException($"Entry amount must not exceed {MaxAmount}.");
        }

        if (decimal.Round(amount, Scale, MidpointRounding.ToZero) != amount)
        {
            throw new InvalidMoneyException($"Entry amount must have at most {Scale} decimal places.");
        }

        return new Money(amount);
    }

    public override string ToString() => Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
