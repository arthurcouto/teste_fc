using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace CashFlow.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly LedgerDomain = typeof(Ledger.Domain.Entry).Assembly;
    private static readonly Assembly LedgerApplication = typeof(Ledger.Application.RecordEntryHandler).Assembly;
    private static readonly Assembly ConsolidationApplication = typeof(Consolidation.Application.DailyBalance).Assembly;
    private static readonly Assembly Contracts = typeof(Contracts.EntryRecorded).Assembly;

    private static readonly Assembly[] InnerLayers =
    [
        LedgerDomain, LedgerApplication, ConsolidationApplication, Contracts
    ];

    private static readonly string[] InfrastructureNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Amazon",
        "Microsoft.AspNetCore",
        "System.Data.Common",
        "System.Net.Http"
    ];

    [Fact]
    public void InnerLayersReferenceNoInfrastructureAssembly()
    {
        var forbidden = new[] { "Microsoft.EntityFrameworkCore", "Npgsql", "AWSSDK", "Microsoft.AspNetCore" };

        foreach (var assembly in InnerLayers)
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

            referenced.ShouldNotContain(
                name => forbidden.Any(f => name.StartsWith(f, StringComparison.Ordinal)),
                $"{assembly.GetName().Name} references an infrastructure assembly.");
        }
    }

    [Fact]
    public void InnerLayersDeclareNoDependencyOnInfrastructureNamespaces()
    {
        foreach (var assembly in InnerLayers)
        {
            Types.InAssembly(assembly)
                .Should().NotHaveDependencyOnAny(InfrastructureNamespaces)
                .GetResult().IsSuccessful.ShouldBeTrue($"{assembly.GetName().Name} depends on infrastructure.");
        }
    }

    [Fact]
    public void DomainReferencesNoOtherProjectOfTheSolution()
    {
        var referenced = LedgerDomain.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        referenced.ShouldNotContain(name => name.StartsWith("CashFlow", StringComparison.Ordinal));
    }

    [Fact]
    public void ContractsReferenceNoOtherProjectOfTheSolution()
    {
        var referenced = Contracts.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        referenced.ShouldNotContain(name => name.StartsWith("CashFlow", StringComparison.Ordinal));
    }

    [Fact]
    public void ConcreteTypesInInnerLayersAreSealed()
    {
        foreach (var assembly in InnerLayers)
        {
            Types.InAssembly(assembly)
                .That().ArePublic().And().AreClasses().And().AreNotAbstract()
                .Should().BeSealed()
                .GetResult().IsSuccessful.ShouldBeTrue($"{assembly.GetName().Name} has an unsealed public class.");
        }
    }

    [Fact]
    public void TypesNamedLikePortsAreInterfaces()
    {
        foreach (var assembly in new[] { LedgerApplication, ConsolidationApplication })
        {
            var offenders = assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsInterface)
                .Where(type => type.Name.Length > 1 && type.Name[0] == 'I' && char.IsUpper(type.Name[1]))
                .Select(type => type.Name);

            offenders.ShouldBeEmpty();
        }
    }

    [Fact]
    public void TimeIsReadThroughTheClockPortAndNeverFromTheAmbientSystem()
    {
        var forbidden = new[]
        {
            ("System.DateTime", "get_UtcNow"),
            ("System.DateTime", "get_Now"),
            ("System.DateTime", "get_Today"),
            ("System.DateTimeOffset", "get_UtcNow"),
            ("System.DateTimeOffset", "get_Now")
        };

        foreach (var assembly in InnerLayers)
        {
            var offenders = AmbientTimeUsage.Find(assembly.Location, forbidden);

            offenders.ShouldBeEmpty($"{assembly.GetName().Name} reads the ambient clock instead of the IClock port.");
        }
    }
}
