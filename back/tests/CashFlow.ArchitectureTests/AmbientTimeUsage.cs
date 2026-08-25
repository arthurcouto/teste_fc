using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace CashFlow.ArchitectureTests;

internal static class AmbientTimeUsage
{
    public static IReadOnlyList<string> Find(string assemblyPath, (string Type, string Member)[] forbidden)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);

        var metadata = peReader.GetMetadataReader();
        var found = new List<string>();

        foreach (var handle in metadata.MemberReferences)
        {
            var reference = metadata.GetMemberReference(handle);

            if (reference.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var declaringType = metadata.GetTypeReference((TypeReferenceHandle)reference.Parent);
            var typeName = $"{metadata.GetString(declaringType.Namespace)}.{metadata.GetString(declaringType.Name)}";
            var memberName = metadata.GetString(reference.Name);

            if (forbidden.Any(f => f.Type == typeName && f.Member == memberName))
            {
                found.Add($"{typeName}.{memberName}");
            }
        }

        return found;
    }
}
