using CashFlow.Ledger.Domain;
using Shouldly;

namespace CashFlow.Ledger.UnitTests;

public sealed class EntryTests
{
    private static readonly DateOnly Today = new(2026, 8, 19);
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);

    private static Entry Record(
        EntryType type = EntryType.Credit,
        decimal amount = 100m,
        DateOnly? competenceDate = null,
        string? description = "daily sale") =>
        Entry.Record(type, Money.Of(amount), competenceDate ?? Today, description, Today, Now);

    [Fact]
    public void RecordsValidEntry()
    {
        var entry = Record(EntryType.Credit, 150.75m);

        entry.Id.ShouldNotBe(Guid.Empty);
        entry.Type.ShouldBe(EntryType.Credit);
        entry.Amount.Amount.ShouldBe(150.75m);
        entry.CompetenceDate.ShouldBe(Today);
        entry.RecordedAt.ShouldBe(Now);
    }

    [Fact]
    public void AcceptsCompetenceOnCurrentDate() =>
        Record(competenceDate: Today).CompetenceDate.ShouldBe(Today);

    [Fact]
    public void AcceptsPastCompetence()
    {
        var yesterday = Today.AddDays(-1);
        Record(competenceDate: yesterday).CompetenceDate.ShouldBe(yesterday);
    }

    [Fact]
    public void RejectsFutureCompetence()
    {
        var error = Should.Throw<InvalidCompetenceDateException>(
            () => Record(competenceDate: Today.AddDays(1)));

        error.Message.ShouldContain("later than");
    }

    [Fact]
    public void RejectsUnknownType() =>
        Should.Throw<InvalidEntryTypeException>(() => Record(type: (EntryType)99));

    [Fact]
    public void RejectsDescriptionAboveLimit() =>
        Should.Throw<InvalidDescriptionException>(
            () => Record(description: new string('x', Entry.DescriptionMaxLength + 1)));

    [Fact]
    public void AcceptsDescriptionAtLimit() =>
        Record(description: new string('x', Entry.DescriptionMaxLength))
            .Description!.Length.ShouldBe(Entry.DescriptionMaxLength);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizesBlankDescriptionToAbsent(string? description) =>
        Record(description: description).Description.ShouldBeNull();

    [Fact]
    public void TrimsDescription() =>
        Record(description: "  sale  ").Description.ShouldBe("sale");

    [Fact]
    public void AssignsUniqueIdentifierToEachEntry() =>
        Record().Id.ShouldNotBe(Record().Id);

    [Fact]
    public void RestoreRejectsEmptyIdentifier() =>
        Should.Throw<CorruptedEntryException>(
            () => Entry.Restore(Guid.Empty, EntryType.Credit, Money.Of(10m), Today, null, Now));

    [Fact]
    public void RestoreRejectsUnknownType() =>
        Should.Throw<CorruptedEntryException>(
            () => Entry.Restore(Guid.NewGuid(), (EntryType)99, Money.Of(10m), Today, null, Now));

    [Fact]
    public void RestoreRejectsOversizedDescription() =>
        Should.Throw<CorruptedEntryException>(
            () => Entry.Restore(
                Guid.NewGuid(), EntryType.Credit, Money.Of(10m), Today,
                new string('x', Entry.DescriptionMaxLength + 1), Now));

    [Fact]
    public void ExposesNoWritablePropertyAfterRecording()
    {
        var writableProperties = typeof(Entry)
            .GetProperties()
            .Where(p => p.CanWrite)
            .Select(p => p.Name);

        writableProperties.ShouldBeEmpty();
    }
}
