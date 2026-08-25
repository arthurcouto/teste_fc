using CashFlow.Consolidation.Api;
using CashFlow.Consolidation.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CashFlow.IntegrationTests;

internal sealed class InMemoryDailyBalanceRepository : IDailyBalanceRepository
{
    private readonly Dictionary<DateOnly, DailyBalance> _balances = [];

    public Task<DailyBalance?> FindAsync(DateOnly competenceDate, CancellationToken cancellationToken) =>
        Task.FromResult(_balances.GetValueOrDefault(competenceDate));

    public Task<IReadOnlyList<DailyBalance>> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DailyBalance> found = [.. _balances.Values
            .Where(balance => balance.CompetenceDate >= startDate && balance.CompetenceDate <= endDate)
            .OrderBy(balance => balance.CompetenceDate)];

        return Task.FromResult(found);
    }

    public Task SaveAsync(DailyBalance balance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(balance);

        _balances[balance.CompetenceDate] = balance;

        return Task.CompletedTask;
    }
}

internal static class InMemoryConsolidation
{
    public static void ReplacePorts(IServiceCollection services, IDailyBalanceRepository balances)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IDailyBalanceRepository>();
        services.AddSingleton(balances);
    }
}

public sealed class ConsolidationApiFactory : WebApplicationFactory<ConsolidationApi>
{
    private readonly InMemoryDailyBalanceRepository _balances = new();

    public async Task SeedAsync(DateOnly competenceDate, decimal credits, decimal debits, int entryCount)
    {
        await _balances.SaveAsync(
            DailyBalance.Restore(
                competenceDate, credits, debits, entryCount, new DateTimeOffset(2026, 3, 15, 9, 0, 0, TimeSpan.Zero)),
            TestContext.Current.CancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("Authentication:Mode", "Disabled");
        builder.UseSetting("Consolidation:ApplyMigrationsOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            InMemoryConsolidation.ReplacePorts(services, _balances);
        });
    }
}
