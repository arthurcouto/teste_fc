using CashFlow.Ledger.Domain;
using Shouldly;

namespace CashFlow.Ledger.UnitTests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void RejectsNonPositiveAmount(decimal amount)
    {
        var error = Should.Throw<InvalidMoneyException>(() => Money.Of(amount));
        error.Message.ShouldContain("greater than zero");
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(1.234)]
    [InlineData(99.999)]
    public void RejectsMoreThanTwoDecimalPlaces(decimal amount)
    {
        var error = Should.Throw<InvalidMoneyException>(() => Money.Of(amount));
        error.Message.ShouldContain("decimal places");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(1234567.89)]
    public void AcceptsPositiveAmountWithUpToTwoDecimalPlaces(decimal amount) =>
        Money.Of(amount).Amount.ShouldBe(amount);

    [Fact]
    public void RejectsAmountAboveTheCeiling() =>
        Should.Throw<InvalidMoneyException>(() => Money.Of(Money.MaxAmount + 1m));

    [Fact]
    public void AcceptsAmountAtTheCeiling() =>
        Money.Of(Money.MaxAmount).Amount.ShouldBe(Money.MaxAmount);

    [Fact]
    public void TreatsDifferentScalesOfTheSameValueAsEqual()
    {
        var oneDecimalPlace = Money.Of(10.5m);
        var twoDecimalPlaces = Money.Of(10.50m);

        oneDecimalPlace.ShouldBe(twoDecimalPlaces);
        oneDecimalPlace.GetHashCode().ShouldBe(twoDecimalPlaces.GetHashCode());
    }
}
