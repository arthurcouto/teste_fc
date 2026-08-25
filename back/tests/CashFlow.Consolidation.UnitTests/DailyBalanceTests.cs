using System.Globalization;
using CashFlow.Consolidation.Application;
using CashFlow.Contracts;
using Shouldly;

namespace CashFlow.Consolidation.UnitTests;

public sealed class DailyBalanceTests
{
    private static readonly DateOnly Date = new(2026, 8, 19);
    private static readonly DateTimeOffset At = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyDayHasZeroedTotals()
    {
        var balance = DailyBalance.EmptyOn(Date);

        balance.TotalCredits.ShouldBe(0m);
        balance.TotalDebits.ShouldBe(0m);
        balance.Balance.ShouldBe(0m);
        balance.EntryCount.ShouldBe(0);
        balance.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void CreditIncreasesBalance()
    {
        var balance = DailyBalance.EmptyOn(Date).Incorporate(EntryTypeContract.Credit, 100.50m, At);

        balance.TotalCredits.ShouldBe(100.50m);
        balance.Balance.ShouldBe(100.50m);
        balance.EntryCount.ShouldBe(1);
        balance.UpdatedAt.ShouldBe(At);
    }

    [Fact]
    public void DebitDecreasesBalance()
    {
        var balance = DailyBalance.EmptyOn(Date).Incorporate(EntryTypeContract.Debit, 40.25m, At);

        balance.TotalDebits.ShouldBe(40.25m);
        balance.Balance.ShouldBe(-40.25m);
    }

    [Fact]
    public void BalanceIsCreditsMinusDebits()
    {
        var balance = DailyBalance.EmptyOn(Date)
            .Incorporate(EntryTypeContract.Credit, 300m, At)
            .Incorporate(EntryTypeContract.Debit, 120.50m, At)
            .Incorporate(EntryTypeContract.Credit, 0.50m, At);

        balance.TotalCredits.ShouldBe(300.50m);
        balance.TotalDebits.ShouldBe(120.50m);
        balance.Balance.ShouldBe(180m);
        balance.EntryCount.ShouldBe(3);
    }

    [Theory]
    [InlineData("0.005")]
    [InlineData("1.234")]
    public void RejectsAmountWithMoreThanTwoDecimalPlaces(string amount) =>
        Should.Throw<UnprocessableEntryException>(
            () => DailyBalance.EmptyOn(Date).Incorporate(
                EntryTypeContract.Credit, decimal.Parse(amount, CultureInfo.InvariantCulture), At));

    [Fact]
    public void RejectsAmountAboveTheCeiling() =>
        Should.Throw<UnprocessableEntryException>(
            () => DailyBalance.EmptyOn(Date).Incorporate(
                EntryTypeContract.Credit, DailyBalance.MaxAmount + 1m, At));

    [Fact]
    public void RestoreRejectsNegativeTotals() =>
        Should.Throw<UnprocessableEntryException>(
            () => DailyBalance.Restore(Date, -1m, 0m, 0, At));

    [Fact]
    public void IncorporationIsOrderIndependent()
    {
        var ascending = DailyBalance.EmptyOn(Date)
            .Incorporate(EntryTypeContract.Credit, 10m, At)
            .Incorporate(EntryTypeContract.Debit, 3m, At)
            .Incorporate(EntryTypeContract.Credit, 7m, At);

        var descending = DailyBalance.EmptyOn(Date)
            .Incorporate(EntryTypeContract.Credit, 7m, At)
            .Incorporate(EntryTypeContract.Debit, 3m, At)
            .Incorporate(EntryTypeContract.Credit, 10m, At);

        descending.Balance.ShouldBe(ascending.Balance);
        descending.TotalCredits.ShouldBe(ascending.TotalCredits);
        descending.TotalDebits.ShouldBe(ascending.TotalDebits);
    }

    [Fact]
    public void PreservesDecimalPrecisionAcrossManyEntries()
    {
        var balance = Enumerable
            .Range(0, 300)
            .Aggregate(
                DailyBalance.EmptyOn(Date),
                (current, _) => current.Incorporate(EntryTypeContract.Credit, 0.10m, At));

        balance.TotalCredits.ShouldBe(30.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveAmount(decimal amount) =>
        Should.Throw<UnprocessableEntryException>(
            () => DailyBalance.EmptyOn(Date).Incorporate(EntryTypeContract.Credit, amount, At));

    [Fact]
    public void RejectsUnknownType() =>
        Should.Throw<UnprocessableEntryException>(
            () => DailyBalance.EmptyOn(Date).Incorporate((EntryTypeContract)99, 10m, At));

    [Fact]
    public void DoesNotMutateTheOriginal()
    {
        var original = DailyBalance.EmptyOn(Date);

        original.Incorporate(EntryTypeContract.Credit, 50m, At);

        original.EntryCount.ShouldBe(0);
    }
}
