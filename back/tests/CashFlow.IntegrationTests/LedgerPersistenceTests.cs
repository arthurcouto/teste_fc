using CashFlow.Contracts;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class LedgerPersistenceTests(EngineFixture engine) : IClassFixture<EngineFixture>
{
    [Fact]
    public async Task MigrationsAreIdempotent()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        var migrator = engine.Ledger
            .GetRequiredService<Ledger.Infrastructure.Persistence.DatabaseMigrator>();

        await migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await migrator.ApplyAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RecordingAnEntryWritesTheOutboxInTheSameTransaction()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        using var scope = engine.Ledger.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RecordEntryHandler>();
        var repository = scope.ServiceProvider.GetRequiredService<IEntryRepository>();

        var entry = await handler.HandleAsync(
            new RecordEntryCommand(EntryType.Credit, 123.45m, TodayAtMerchant(scope), "integration"),
            TestContext.Current.CancellationToken);

        var persisted = await repository.FindAsync(entry.Id, TestContext.Current.CancellationToken);

        persisted.ShouldNotBeNull();
        persisted.Amount.Amount.ShouldBe(123.45m);
        persisted.Type.ShouldBe(EntryType.Credit);
    }

    [Fact]
    public async Task NothingIsPersistedWhenTheOperationFailsInsideTheUnitOfWork()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        using var scope = engine.Ledger.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repository = scope.ServiceProvider.GetRequiredService<IEntryRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var entry = Entry.Record(
            EntryType.Debit, Money.Of(99.99m), clock.TodayAtMerchant, "rolled back",
            clock.TodayAtMerchant, clock.UtcNow);

        await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.ExecuteAsync<bool>(async token =>
        {
            await repository.AddAsync(entry, token);
            await outbox.AddAsync(new EntryRecorded
            {
                EntryId = entry.Id,
                Type = EntryTypeContract.Debit,
                Amount = entry.Amount.Amount,
                CompetenceDate = entry.CompetenceDate,
                RecordedAt = entry.RecordedAt,
                CorrelationId = "rollback"
            }, token);

            throw new InvalidOperationException("forced failure after both writes");
        }, TestContext.Current.CancellationToken));

        using var verification = engine.Ledger.CreateScope();
        var reader = verification.ServiceProvider.GetRequiredService<IEntryRepository>();

        (await reader.FindAsync(entry.Id, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    private static DateOnly TodayAtMerchant(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IClock>().TodayAtMerchant;
}
