using CashFlow.Contracts;
using CashFlow.Ledger.Api;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CashFlow.IntegrationTests;

internal sealed class InMemoryEntryRepository : IEntryRepository
{
    private readonly List<Entry> _entries = [];

    public Task AddAsync(Entry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<Entry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.SingleOrDefault(entry => entry.Id == id));

    public Task<EntryPage> ListOrderedByCompetenceThenRecordedAtAsync(
        EntryPeriod period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);

        var matching = _entries
            .Where(entry => entry.CompetenceDate >= period.From && entry.CompetenceDate <= period.To)
            .OrderBy(entry => entry.CompetenceDate)
            .ThenBy(entry => entry.RecordedAt)
            .ToList();

        var page = matching.Skip(period.Offset).Take(period.Limit).ToList();

        return Task.FromResult(new EntryPage(page, matching.Count));
    }
}

internal sealed class InMemoryOutbox : IOutbox
{
    private readonly List<EntryRecorded> _events = [];

    public IReadOnlyList<EntryRecorded> Events => _events;

    public Task AddAsync(EntryRecorded integrationEvent, CancellationToken cancellationToken)
    {
        _events.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

internal sealed class DirectUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation(cancellationToken);
    }
}

internal sealed class StoppedClock(DateTimeOffset utcNow, DateOnly todayAtMerchant) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;

    public DateOnly TodayAtMerchant { get; } = todayAtMerchant;
}

internal static class InMemoryLedger
{
    public static readonly DateOnly TodayAtMerchant = new(2026, 3, 15);

    public static readonly DateTimeOffset Now = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    public static void ReplacePorts(IServiceCollection services, IEntryRepository entries)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IEntryRepository>();
        services.RemoveAll<IOutbox>();
        services.RemoveAll<IUnitOfWork>();
        services.RemoveAll<IClock>();

        services.AddSingleton(entries);
        services.AddSingleton<IOutbox, InMemoryOutbox>();
        services.AddSingleton<IUnitOfWork, DirectUnitOfWork>();
        services.AddSingleton<IClock>(new StoppedClock(Now, TodayAtMerchant));
    }
}

public sealed class LedgerApiFactory : WebApplicationFactory<LedgerApi>
{
    private readonly InMemoryEntryRepository _entries = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("Authentication:Mode", "Disabled");
        builder.UseSetting("Ledger:ApplyMigrationsOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            InMemoryLedger.ReplacePorts(services, _entries);
        });
    }
}
