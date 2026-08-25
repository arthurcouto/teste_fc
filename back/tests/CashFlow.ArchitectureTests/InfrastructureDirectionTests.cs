using System.Reflection;
using Shouldly;

namespace CashFlow.ArchitectureTests;

public sealed class InfrastructureDirectionTests
{
    private static readonly Assembly LedgerApplication = typeof(Ledger.Application.RecordEntryHandler).Assembly;
    private static readonly Assembly ConsolidationApplication = typeof(Consolidation.Application.DailyBalance).Assembly;
    private static readonly Assembly LedgerInfrastructure = typeof(Ledger.Infrastructure.LedgerInfrastructureOptions).Assembly;
    private static readonly Assembly ConsolidationInfrastructure =
        typeof(Consolidation.Infrastructure.ConsolidationInfrastructureOptions).Assembly;

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructure()
    {
        foreach (var assembly in new[] { LedgerApplication, ConsolidationApplication })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

            referenced.ShouldNotContain(name => name.EndsWith(".Infrastructure", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void InfrastructureOfOneServiceDoesNotReferenceTheOther()
    {
        LedgerInfrastructure.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)
            .ShouldNotContain(name => name.StartsWith("CashFlow.Consolidation", StringComparison.Ordinal));

        ConsolidationInfrastructure.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)
            .ShouldNotContain(name => name.StartsWith("CashFlow.Ledger", StringComparison.Ordinal));
    }

    [Fact]
    public void InfrastructureExposesOnlyItsCompositionSurface()
    {
        foreach (var assembly in new[] { LedgerInfrastructure, ConsolidationInfrastructure })
        {
            var publicTypes = assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsNested)
                .Select(type => type.Name)
                .Where(name => name is not ("DependencyInjection" or "DatabaseMigrator" or "MigrationFailedException"
                    or "SqlStatements" or "AmbientCorrelationContext" or "DatabaseEngine"))
                .Where(name => !name.EndsWith("InfrastructureOptions", StringComparison.Ordinal));

            publicTypes.ShouldBeEmpty($"{assembly.GetName().Name} leaks an implementation type as public API.");
        }
    }
}
