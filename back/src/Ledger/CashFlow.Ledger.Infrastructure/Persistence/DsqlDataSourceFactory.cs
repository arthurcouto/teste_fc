using Amazon;
using Amazon.DSQL.Util;
using Npgsql;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal static class DsqlDataSourceFactory
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshBefore = TimeSpan.FromMinutes(5);

    public static NpgsqlDataSource Create(LedgerInfrastructureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = options.DatabaseHost,
            Port = options.DatabasePort,
            Database = options.DatabaseName,
            Username = options.DatabaseUser,
            MaxPoolSize = options.MaxPoolSize,
            ConnectionLifetime = JitteredLifetime(options.ConnectionLifetimeSeconds)
        };

        if (options.Engine is DatabaseEngine.PostgreSql)
        {
            connection.Password = options.DatabasePassword;
            connection.SslMode = SslMode.Disable;
            return new NpgsqlDataSourceBuilder(connection.ConnectionString).Build();
        }

        connection.SslMode = SslMode.VerifyFull;
        connection.NoResetOnClose = true;

        var region = RegionEndpoint.GetBySystemName(options.Region);
        var builder = new NpgsqlDataSourceBuilder(connection.ConnectionString);

        builder.UsePeriodicPasswordProvider(
            (_, _) => new ValueTask<string>(
                DSQLAuthTokenGenerator.GenerateDbConnectAdminAuthToken(region, options.DatabaseHost, TokenLifetime)),
            TokenLifetime - RefreshBefore,
            TimeSpan.FromSeconds(5));

        return builder.Build();
    }

    private static int JitteredLifetime(int baseSeconds) =>
        baseSeconds + Random.Shared.Next(-baseSeconds / 10, baseSeconds / 10);
}
