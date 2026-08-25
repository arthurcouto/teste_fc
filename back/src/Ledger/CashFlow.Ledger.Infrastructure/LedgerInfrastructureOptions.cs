namespace CashFlow.Ledger.Infrastructure;

public sealed class LedgerInfrastructureOptions
{
    public const string SectionName = "Ledger";

    public DatabaseEngine Engine { get; set; } = DatabaseEngine.AuroraDsql;

    public string DatabaseHost { get; set; } = string.Empty;

    public int DatabasePort { get; set; } = 5432;

    public string? DatabasePassword { get; set; }

    public string? QueueServiceUrl { get; set; }

    public string DatabaseName { get; set; } = "postgres";

    public string DatabaseUser { get; set; } = "admin";

    public string Region { get; set; } = "us-east-1";

    public string MerchantTimeZone { get; set; } = "America/Sao_Paulo";

    public string QueueUrl { get; set; } = string.Empty;

    public int MaxPoolSize { get; set; } = 20;

    public int ConnectionLifetimeSeconds { get; set; } = 1500;

    public int TransactionMaxAttempts { get; set; } = 12;

    public int OutboxBatchSize { get; set; } = 25;

    public int OutboxPollSeconds { get; set; } = 3;

    public int OutboxClaimExpirySeconds { get; set; } = 300;

    public int OutboxMaxAttempts { get; set; } = 5;
}
