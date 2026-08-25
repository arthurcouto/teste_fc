namespace CashFlow.Ledger.Infrastructure.Messaging;

internal static class InstanceIdentifier
{
    public const int MaxLength = 64;

    public static string Current { get; } = Build();

    private static string Build()
    {
        var candidate = $"{Environment.MachineName}-{Guid.NewGuid():N}";
        return candidate.Length <= MaxLength ? candidate : candidate[^MaxLength..];
    }
}
