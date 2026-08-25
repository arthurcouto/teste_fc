using System.Reflection;
using Shouldly;

namespace CashFlow.ArchitectureTests;

public sealed class MigrationTests
{
    private static readonly Assembly[] InfrastructureAssemblies =
    [
        typeof(Ledger.Infrastructure.LedgerInfrastructureOptions).Assembly,
        typeof(Consolidation.Infrastructure.ConsolidationInfrastructureOptions).Assembly
    ];

    private static IEnumerable<(string Name, string Script)> Migrations() =>
        InfrastructureAssemblies.SelectMany(assembly => assembly.GetManifestResourceNames()
            .Where(resource => resource.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(resource =>
            {
                using var stream = assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream);
                return (resource, reader.ReadToEnd());
            }));

    [Fact]
    public void MigrationsAreEmbeddedAndDiscoverable() =>
        Migrations().ShouldNotBeEmpty();

    [Fact]
    public void NoMigrationCreatesAnIndexSynchronously()
    {
        foreach (var (name, script) in Migrations())
        {
            var offenders = Ledger.Infrastructure.Persistence.SqlStatements.Split(script)
                .Where(Ledger.Infrastructure.Persistence.SqlStatements.IsSynchronousIndex);

            offenders.ShouldBeEmpty(
                $"{name} creates an index without ASYNC, which the engine rejects.");
        }
    }

    [Fact]
    public void NoMigrationDeclaresAForeignKey()
    {
        foreach (var (name, script) in Migrations())
        {
            script.ShouldNotContain("REFERENCES", Case.Insensitive,
                $"{name} declares a foreign key, which the engine does not support.");
        }
    }

    [Fact]
    public void NoMigrationCreatesAPartialIndex()
    {
        foreach (var (name, script) in Migrations())
        {
            var offenders = Ledger.Infrastructure.Persistence.SqlStatements.Split(script)
                .Where(statement =>
                    statement.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase)
                    && statement.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase));

            offenders.ShouldBeEmpty($"{name} creates a partial index, which the engine does not support.");
        }
    }

    [Fact]
    public void NoMigrationCreatesASequenceWithoutAnExplicitCache()
    {
        foreach (var (name, script) in Migrations())
        {
            var offenders = Ledger.Infrastructure.Persistence.SqlStatements.Split(script)
                .Where(statement =>
                    statement.Contains("CREATE SEQUENCE", StringComparison.OrdinalIgnoreCase)
                    && !statement.Contains("CACHE", StringComparison.OrdinalIgnoreCase));

            offenders.ShouldBeEmpty($"{name} creates a sequence without an explicit cache size.");
        }
    }
}
