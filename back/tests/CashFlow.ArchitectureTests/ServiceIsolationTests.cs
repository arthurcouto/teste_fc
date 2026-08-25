using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace CashFlow.ArchitectureTests;

public sealed class ServiceIsolationTests
{
    private static readonly Assembly LedgerDomain = typeof(Ledger.Domain.Entry).Assembly;
    private static readonly Assembly LedgerApplication = typeof(Ledger.Application.RecordEntryHandler).Assembly;
    private static readonly Assembly ConsolidationApplication = typeof(Consolidation.Application.DailyBalance).Assembly;
    private static readonly Assembly LedgerApi = typeof(Ledger.Api.LedgerApi).Assembly;
    private static readonly Assembly ConsolidationApi = typeof(Consolidation.Api.ConsolidationApi).Assembly;

    [Fact]
    public void LedgerDoesNotDependOnConsolidation()
    {
        foreach (var assembly in new[] { LedgerDomain, LedgerApplication })
        {
            Types.InAssembly(assembly)
                .Should().NotHaveDependencyOn("CashFlow.Consolidation")
                .GetResult().FailingTypeNames.ShouldBeNull();
        }
    }

    [Fact]
    public void ConsolidationDoesNotDependOnLedger() =>
        Types.InAssembly(ConsolidationApplication)
            .Should().NotHaveDependencyOnAny(["CashFlow.Ledger.Domain", "CashFlow.Ledger.Application"])
            .GetResult().FailingTypeNames.ShouldBeNull();

    [Fact]
    public void NeitherServiceReferencesTheOtherAssembly()
    {
        var ledgerReferences = LedgerApplication.GetReferencedAssemblies().Select(a => a.Name);
        var consolidationReferences = ConsolidationApplication.GetReferencedAssemblies().Select(a => a.Name);

        ledgerReferences.ShouldNotContain("CashFlow.Consolidation.Application");
        consolidationReferences.ShouldNotContain("CashFlow.Ledger.Domain");
        consolidationReferences.ShouldNotContain("CashFlow.Ledger.Application");
    }

    [Fact]
    public void ServicesShareOnlyTheIntegrationContract()
    {
        var shared = LedgerApplication.GetReferencedAssemblies().Select(a => a.Name)
            .Intersect(ConsolidationApplication.GetReferencedAssemblies().Select(a => a.Name))
            .Where(name => name is not null && name.StartsWith("CashFlow", StringComparison.Ordinal));

        shared.ShouldBe(["CashFlow.Contracts"]);
    }

    [Fact]
    public void NeitherPresentationLayerReferencesTheOtherServiceAssembly()
    {
        SolutionReferencesOf(LedgerApi).ShouldNotContain(
            name => name.StartsWith("CashFlow.Consolidation", StringComparison.Ordinal),
            "The ledger presentation layer reaches into the consolidation service.");

        SolutionReferencesOf(ConsolidationApi).ShouldNotContain(
            name => name.StartsWith("CashFlow.Ledger", StringComparison.Ordinal),
            "The consolidation presentation layer reaches into the ledger service.");
    }

    [Fact]
    public void NeitherPresentationLayerDeclaresADependencyOnTheOtherService()
    {
        Types.InAssembly(LedgerApi)
            .Should().NotHaveDependencyOn("CashFlow.Consolidation")
            .GetResult().FailingTypeNames.ShouldBeNull();

        Types.InAssembly(ConsolidationApi)
            .Should().NotHaveDependencyOn("CashFlow.Ledger")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void PresentationLayersShareOnlyTheIntegrationContract()
    {
        var shared = SolutionReferencesOf(LedgerApi).Intersect(SolutionReferencesOf(ConsolidationApi));

        shared.ShouldBeSubsetOf(["CashFlow.Contracts"]);
    }

    private static IEnumerable<string> SolutionReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("CashFlow", StringComparison.Ordinal));
}
