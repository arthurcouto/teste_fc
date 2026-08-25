using CashFlow.Contracts;

namespace CashFlow.Consolidation.Application;

public sealed record DailyBalance
{
    private DailyBalance(
        DateOnly competenceDate,
        decimal totalCredits,
        decimal totalDebits,
        int entryCount,
        DateTimeOffset? updatedAt)
    {
        CompetenceDate = competenceDate;
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        EntryCount = entryCount;
        UpdatedAt = updatedAt;
    }

    public DateOnly CompetenceDate { get; }

    public decimal TotalCredits { get; }

    public decimal TotalDebits { get; }

    public int EntryCount { get; }

    public DateTimeOffset? UpdatedAt { get; }

    public decimal Balance => TotalCredits - TotalDebits;

    public static DailyBalance EmptyOn(DateOnly competenceDate) =>
        new(competenceDate, 0m, 0m, 0, null);

    public static DailyBalance Restore(
        DateOnly competenceDate,
        decimal totalCredits,
        decimal totalDebits,
        int entryCount,
        DateTimeOffset updatedAt)
    {
        if (totalCredits < 0 || totalDebits < 0 || entryCount < 0)
        {
            throw new UnprocessableEntryException(
                $"A stored balance for {competenceDate:yyyy-MM-dd} has negative totals or count.");
        }

        return new DailyBalance(competenceDate, totalCredits, totalDebits, entryCount, updatedAt);
    }

    public const int Scale = 2;

    public const decimal MaxAmount = 99_999_999_999_999_999m;

    public DailyBalance Incorporate(EntryTypeContract type, decimal amount, DateTimeOffset at)
    {
        if (amount <= 0)
        {
            throw new UnprocessableEntryException($"Entry amount must be greater than zero, but was {amount}.");
        }

        if (amount > MaxAmount)
        {
            throw new UnprocessableEntryException($"Entry amount must not exceed {MaxAmount}, but was {amount}.");
        }

        if (decimal.Round(amount, Scale, MidpointRounding.ToZero) != amount)
        {
            throw new UnprocessableEntryException(
                $"Entry amount must have at most {Scale} decimal places, but was {amount}.");
        }

        return type switch
        {
            EntryTypeContract.Credit => new DailyBalance(
                CompetenceDate, Accumulate(TotalCredits, amount), TotalDebits, EntryCount + 1, at),
            EntryTypeContract.Debit => new DailyBalance(
                CompetenceDate, TotalCredits, Accumulate(TotalDebits, amount), EntryCount + 1, at),
            _ => throw new UnprocessableEntryException($"Unknown entry type: {type}.")
        };
    }

    private static decimal Accumulate(decimal total, decimal amount)
    {
        var accumulated = total + amount;

        if (accumulated > MaxAmount)
        {
            throw new UnprocessableEntryException(
                $"Accumulated total must not exceed {MaxAmount}, but would be {accumulated}.");
        }

        return accumulated;
    }
}
