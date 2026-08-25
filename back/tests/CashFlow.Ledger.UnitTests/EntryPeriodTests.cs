using CashFlow.Ledger.Application;
using Shouldly;

namespace CashFlow.Ledger.UnitTests;

public sealed class EntryPeriodTests
{
    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 19);

    [Fact]
    public void RejectsInvertedPeriod() =>
        Should.Throw<RequestValidationException>(() => EntryPeriod.Of(To, From));

    [Fact]
    public void AcceptsSingleDayPeriod() =>
        EntryPeriod.Of(From, From).From.ShouldBe(From);

    [Fact]
    public void AppliesDefaultPagination()
    {
        var period = EntryPeriod.Of(From, To);

        period.Offset.ShouldBe(0);
        period.Limit.ShouldBe(EntryPeriod.DefaultPageSize);
    }

    [Fact]
    public void RejectsNegativeOffset() =>
        Should.Throw<RequestValidationException>(() => EntryPeriod.Of(From, To, offset: -1));

    [Theory]
    [InlineData(0)]
    [InlineData(EntryPeriod.MaxPageSize + 1)]
    public void RejectsLimitOutsideTheAllowedRange(int limit) =>
        Should.Throw<RequestValidationException>(() => EntryPeriod.Of(From, To, limit: limit));

    [Theory]
    [InlineData(1)]
    [InlineData(EntryPeriod.MaxPageSize)]
    public void AcceptsLimitAtTheBoundaries(int limit) =>
        EntryPeriod.Of(From, To, limit: limit).Limit.ShouldBe(limit);
}
