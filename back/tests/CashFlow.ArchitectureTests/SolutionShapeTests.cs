using System.Reflection;
using Shouldly;

namespace CashFlow.ArchitectureTests;

public sealed class SolutionShapeTests
{
    private static readonly Assembly LedgerDomain = typeof(Ledger.Domain.Entry).Assembly;
    private static readonly Assembly LedgerApplication = typeof(Ledger.Application.RecordEntryHandler).Assembly;
    private static readonly Assembly ConsolidationApplication = typeof(Consolidation.Application.DailyBalance).Assembly;

    [Fact]
    public void EveryDomainExceptionDescendsFromTheDomainBase()
    {
        var exceptions = LedgerDomain.GetTypes()
            .Where(type => typeof(Exception).IsAssignableFrom(type) && type != typeof(Ledger.Domain.DomainException));

        foreach (var exception in exceptions)
        {
            typeof(Ledger.Domain.DomainException).IsAssignableFrom(exception).ShouldBeTrue(
                $"{exception.Name} does not descend from DomainException.");
        }
    }

    [Fact]
    public void AsynchronousPortOperationsAcceptCancellation()
    {
        foreach (var assembly in new[] { LedgerApplication, ConsolidationApplication })
        {
            var operations = assembly.GetTypes()
                .Where(type => type.IsInterface && type.IsPublic)
                .SelectMany(type => type.GetMethods())
                .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType));

            foreach (var operation in operations)
            {
                operation.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(CancellationToken))
                    .ShouldBeTrue($"{operation.DeclaringType?.Name}.{operation.Name} does not accept cancellation.");
            }
        }
    }

    [Fact]
    public void DomainTypesExposeNoWritableProperty()
    {
        var writable = LedgerDomain.GetTypes()
            .Where(type => type.IsPublic && type.IsClass && !typeof(Exception).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.CanWrite)
            .Select(property => $"{property.DeclaringType?.Name}.{property.Name}");

        writable.ShouldBeEmpty();
    }
}
