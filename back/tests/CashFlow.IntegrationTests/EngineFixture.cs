using CashFlow.Consolidation.Infrastructure;
using CashFlow.Ledger.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CashFlow.IntegrationTests;

public sealed class EngineFixture : IAsyncLifetime
{
    public const string SkipReason =
        "Set LEDGER_DB_HOST and CONSOLIDATION_DB_HOST to run the integration suite against a real engine.";

    public static string? LedgerHost => Environment.GetEnvironmentVariable("LEDGER_DB_HOST");

    public static string? ConsolidationHost => Environment.GetEnvironmentVariable("CONSOLIDATION_DB_HOST");

    public static string Region => Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

    public static bool Available => !string.IsNullOrWhiteSpace(LedgerHost)
                                    && !string.IsNullOrWhiteSpace(ConsolidationHost);

    public ServiceProvider Ledger { get; private set; } = null!;

    public ServiceProvider Consolidation { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        if (!Available)
        {
            return;
        }

        Ledger = BuildLedger();
        Consolidation = BuildConsolidation();

        await Ledger.GetRequiredService<Ledger.Infrastructure.Persistence.DatabaseMigrator>()
            .ApplyAsync(TestContext.Current.CancellationToken);
        await Consolidation.GetRequiredService<Consolidation.Infrastructure.Persistence.DatabaseMigrator>()
            .ApplyAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Ledger is not null)
        {
            await Ledger.DisposeAsync();
        }

        if (Consolidation is not null)
        {
            await Consolidation.DisposeAsync();
        }
    }

    private static void RemoveHostedServices(ServiceCollection services)
    {
        var hosted = services
            .Where(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .ToList();

        foreach (var descriptor in hosted)
        {
            services.Remove(descriptor);
        }
    }

    private static ServiceProvider BuildLedger()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ledger:DatabaseHost"] = LedgerHost,
            ["Ledger:Region"] = Region,
            ["Ledger:QueueUrl"] = "https://sqs.invalid/queue",
            ["Ledger:MerchantTimeZone"] = "America/Sao_Paulo"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddLedgerInfrastructure(configuration);
        RemoveHostedServices(services);

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildConsolidation()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Consolidation:DatabaseHost"] = ConsolidationHost,
            ["Consolidation:Region"] = Region,
            ["Consolidation:QueueUrl"] = "https://sqs.invalid/queue"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddConsolidationInfrastructure(configuration);
        RemoveHostedServices(services);

        return services.BuildServiceProvider();
    }
}
