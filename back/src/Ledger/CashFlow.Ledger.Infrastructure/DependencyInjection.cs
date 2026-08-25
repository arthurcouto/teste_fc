using Amazon;
using Amazon.SQS;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Infrastructure.Correlation;
using CashFlow.Ledger.Infrastructure.Messaging;
using CashFlow.Ledger.Infrastructure.Persistence;
using CashFlow.Ledger.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CashFlow.Ledger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new LedgerInfrastructureOptions();
        configuration.GetSection(LedgerInfrastructureOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAmazonSQS>(_ => CreateSqsClient(options));

        services.AddSingleton(_ => DsqlDataSourceFactory.Create(options));

        services.AddDbContext<LedgerDbContext>((provider, builder) =>
            builder.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));

        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<ICorrelationContext, AmbientCorrelationContext>();

        services.AddScoped<IClock, MerchantClock>();
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, SqsIntegrationEventPublisher>();
        services.AddScoped<OutboxPublisher>();

        services.AddScoped<RecordEntryHandler>();
        services.AddScoped<GetEntryHandler>();
        services.AddScoped<ListEntriesHandler>();

        services.AddHostedService<OutboxPublisherService>();

        return services;
    }

    private static AmazonSQSClient CreateSqsClient(LedgerInfrastructureOptions options)
    {
        var configuration = new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };

        if (!string.IsNullOrWhiteSpace(options.QueueServiceUrl))
        {
            configuration.ServiceURL = options.QueueServiceUrl;
            configuration.AuthenticationRegion = options.Region;
        }

        return new AmazonSQSClient(configuration);
    }
}
