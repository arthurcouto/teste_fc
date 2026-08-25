namespace CashFlow.Consolidation.Infrastructure.Persistence;

public static class SqlStatements
{
    public static IReadOnlyList<string> Split(string script) =>
        [.. script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => statement.Length > 0)];

    public static bool IsAsynchronousIndex(string statement) =>
        statement.Contains("CREATE INDEX ASYNC", StringComparison.OrdinalIgnoreCase);

    public static string ForEngine(string statement, DatabaseEngine engine) =>
        engine is DatabaseEngine.PostgreSql
            ? statement.Replace("CREATE INDEX ASYNC", "CREATE INDEX", StringComparison.OrdinalIgnoreCase)
            : statement;

    public static bool IsSynchronousIndex(string statement) =>
        statement.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase)
        && !IsAsynchronousIndex(statement);
}
