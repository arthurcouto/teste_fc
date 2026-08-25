using Amazon;
using Amazon.SQS;
using CashFlow.Consolidation.Application;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Persistence;
using CashFlow.Consolidation.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CashFlow.Consolidation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConsolidationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ConsolidationInfrastructureOptions();
        configuration.GetSection(ConsolidationInfrastructureOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAmazonSQS>(_ => CreateSqsClient(options));

        services.AddSingleton(_ => DsqlDataSourceFactory.Create(options));

        services.AddDbContext<ConsolidationDbContext>((provider, builder) =>
            builder.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));

        services.AddSingleton<DatabaseMigrator>();

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();
        services.AddScoped<IProcessedEntryLog, ProcessedEntryLog>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IncorporateEntryHandler>();
        services.AddScoped<GetDailyBalanceHandler>();
        services.AddScoped<GetBalanceSeriesHandler>();

        services.AddHostedService<EntryRecordedConsumer>();

        return services;
    }

    private static AmazonSQSClient CreateSqsClient(ConsolidationInfrastructureOptions options)
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
