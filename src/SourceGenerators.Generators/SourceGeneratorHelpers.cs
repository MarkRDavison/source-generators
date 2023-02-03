using Microsoft.CodeAnalysis;
using System.Text;

namespace SourceGenerators.Generators;

public static class SourceGeneratorHelpers
{
    public static string GetNamespace(ITypeSymbol syntax)
    {
        string nameSpace = string.Empty;

        INamespaceSymbol? namespaceSymbol = syntax.ContainingNamespace;

        while (!string.IsNullOrEmpty(namespaceSymbol?.Name))
        {
            nameSpace = namespaceSymbol!.Name + (string.IsNullOrEmpty(nameSpace) ? "" : ".") + nameSpace;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return nameSpace;
    }

    public static IEnumerable<ITypeSymbol> GetAllTypes(INamespaceSymbol root)
    {
        foreach (var namespaceOrTypeSymbol in root.GetMembers())
        {
            if (namespaceOrTypeSymbol is INamespaceSymbol @namespace)
            {
                foreach (var nested in GetAllTypes(@namespace))
                {
                    yield return nested;
                }
            }

            else if (namespaceOrTypeSymbol is ITypeSymbol type)
            {
                yield return type;
            }
        }
    }

    public static List<ITypeSymbol> GetPotentialTypeSymbols(GeneratorExecutionContext context, HashSet<string> namespaces)
    {
        return context.Compilation.SourceModule.ReferencedAssemblySymbols
            .Where(_ => namespaces.Any(__ => _.Identity.Name.StartsWith(__)))
            .SelectMany(_ =>
            {
                try
                {
                    var main = _.Identity.Name.Split('.').Aggregate(_.GlobalNamespace, (s, c) => s.GetNamespaceMembers().Single(m => m.Name.Equals(c)));

                    return SourceGeneratorHelpers.GetAllTypes(main);
                }
                catch
                {
                    return Enumerable.Empty<ITypeSymbol>();
                }
            })
            .ToList();
    }

    public static string Attribute = @"
namespace NetEscapades.EnumGenerators
{
    [System.AttributeUsage(System.AttributeTargets.Enum)]
    public class EnumExtensionsAttribute : System.Attribute
    {
    }
}";

    public static string GenerateExtensionClass(List<EnumToGenerate> enumsToGenerate)
    {
        var sb = new StringBuilder();
        sb.Append(@"
namespace NetEscapades.EnumGenerators
{
    public static partial class EnumExtensions
    {");
        foreach (var enumToGenerate in enumsToGenerate)
        {
            sb.Append(@"
        public static string ToStringFast(this ").Append(enumToGenerate.Name).Append(@" value)
            => value switch
            {");
            foreach (var member in enumToGenerate.Values)
            {
                sb.Append(@"
        ").Append(enumToGenerate.Name).Append('.').Append(member)
                    .Append(" => nameof(")
                    .Append(enumToGenerate.Name).Append('.').Append(member).Append("),");
            }

            sb.Append(@"
            _ => value.ToString(),
        };
");
        }

        sb.Append(@"
    }
}");

        return sb.ToString();
    }
}
