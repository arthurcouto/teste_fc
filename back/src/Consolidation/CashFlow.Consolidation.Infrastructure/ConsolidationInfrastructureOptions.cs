namespace CashFlow.Consolidation.Infrastructure;

public sealed class ConsolidationInfrastructureOptions
{
    public const string SectionName = "Consolidation";

    public DatabaseEngine Engine { get; set; } = DatabaseEngine.AuroraDsql;

    public string DatabaseHost { get; set; } = string.Empty;

    public int DatabasePort { get; set; } = 5432;

    public string? DatabasePassword { get; set; }

    public string? QueueServiceUrl { get; set; }

    public string DatabaseName { get; set; } = "postgres";

    public string DatabaseUser { get; set; } = "admin";

    public string Region { get; set; } = "us-east-1";

    public string QueueUrl { get; set; } = string.Empty;

    public int MaxPoolSize { get; set; } = 20;

    public int ConnectionLifetimeSeconds { get; set; } = 1500;

    public int TransactionMaxAttempts { get; set; } = 12;

    public int ReceiveWaitSeconds { get; set; } = 20;

    public int VisibilityBackoffSeconds { get; set; } = 30;
}
