using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGenerators.Generators.CQRS;



[Generator]
public class CQRSGenerator : ISourceGenerator
{
    // determine the namespace the class/enum/struct is declared in, if any
    static string GetNamespace(ITypeSymbol syntax)
    {
        // If we don't have a namespace at all we'll return an empty string
        // This accounts for the "default namespace" case
        string nameSpace = string.Empty;

        INamespaceSymbol? namespaceSymbol = syntax.ContainingNamespace;

        while (!string.IsNullOrEmpty(namespaceSymbol?.Name))
        {

            nameSpace = namespaceSymbol!.Name + (string.IsNullOrEmpty(nameSpace) ? "" : ".") + nameSpace;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        // return the final namespace
        return nameSpace;
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var symbols = context.Compilation.SourceModule.ReferencedAssemblySymbols
            .Where(_ => _.Identity.Name.StartsWith("SourceGenerators"))
            .SelectMany(_ =>
            {
                try
                {
                    var main = _.Identity.Name.Split('.').Aggregate(_.GlobalNamespace, (s, c) => s.GetNamespaceMembers().Single(m => m.Name.Equals(c)));

                    var types = GetAllTypes(main);
                    foreach (var type in types)
                    {
                        foreach (var syntax in type.DeclaringSyntaxReferences)
                        {
                            if (syntax.GetSyntax() is ClassDeclarationSyntax cds)
                            {

                            }
                        }
                    }
                    return types;
                }
                catch
                {
                    return Enumerable.Empty<ITypeSymbol>();
                }
            })
            .ToList();

        bool present = false;

        StringBuilder diStringBuilder = new StringBuilder();

        var diNamespaces = new HashSet<string>();
        var commandHandlers = new HashSet<string>();
        diNamespaces.Add("Microsoft.Extensions.DependencyInjection");
        diNamespaces.Add("SourceGenerators.Common.CQRS");

        foreach (var symbol in symbols)
        {
            var commandInterface = symbol.Interfaces.FirstOrDefault(_ => _.Name.Equals("ICommand") && _.TypeArguments.Length == 2);
            var commandHandler = symbols.FirstOrDefault(_ =>
            {
                return null != _.Interfaces.FirstOrDefault(_ =>
                    _.Name.Equals("ICommandHandler") &&
                    _.TypeArguments.Length == 2 &&
                    _.TypeArguments[0].Name == commandInterface?.TypeArguments[0]?.Name &&
                    _.TypeArguments[1].Name == commandInterface?.TypeArguments[1]?.Name);
            });
            if (commandInterface != null && commandHandler != null)
            {
                present = true;
                commandHandlers.Add($"services.AddScoped<ICommandHandler<{commandInterface.TypeArguments[0].Name},{commandInterface.TypeArguments[1].Name}>, {commandHandler.Name}>()");
                diNamespaces.Add(GetNamespace(commandHandler));
                diNamespaces.Add(GetNamespace(commandInterface.TypeArguments[0]));
                diNamespaces.Add(GetNamespace(commandInterface.TypeArguments[1]));
            }
        }

        if (present)
        {

            foreach (var n in diNamespaces.OrderBy(_ => _))
            {
                diStringBuilder.AppendLine($"using {n};");
            }

            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("namespace SourceGenerators.Test {");
            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("    public static class CQRSDependecyInjectionExtensions {");
            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("        public static void UseCQRS(this IServiceCollection services) {");
            diStringBuilder.AppendLine(string.Empty);

            foreach (var ch in commandHandlers.OrderBy(_ => _))
            {
                diStringBuilder.AppendLine($"            {ch};");
            }

            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("        }");
            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("    }");
            diStringBuilder.AppendLine(string.Empty);
            diStringBuilder.AppendLine("}");

            context.AddSource("CQRSDependecyInjectionExtensions.g.cs", SourceText.From(diStringBuilder.ToString(), Encoding.UTF8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {

    }

    private static IEnumerable<ITypeSymbol> GetAllTypes(INamespaceSymbol root)
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

    //    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    //    {
    //        var symbols = context.SemanticModel.Compilation.SourceModule.ReferencedAssemblySymbols
    //            .Where(_ => _.Identity.Name.StartsWith("SourceGenerators"))
    //            .SelectMany(_ =>
    //            {
    //                try
    //                {
    //                    var main = _.Identity.Name.Split('.').Aggregate(_.GlobalNamespace, (s, c) => s.GetNamespaceMembers().Single(m => m.Name.Equals(c)));

    //                    var types = GetAllTypes(main);
    //                    foreach (var type in types)
    //                    {
    //                        foreach (var syntax in type.DeclaringSyntaxReferences)
    //                        {
    //                            if (syntax.GetSyntax() is ClassDeclarationSyntax cds)
    //                            {

    //                            }
    //                        }
    //                    }
    //                    return types;
    //                }
    //                catch
    //                {
    //                    return Enumerable.Empty<ITypeSymbol>();
    //                }
    //            })
    //            .ToList();

    //        if (symbols.Any())
    //        {
    //        }

    //        List<string> commands = new List<string>();

    //        foreach (var symbol in symbols)// context.SemanticModel.Compilation.SourceModule.ReferencedAssemblySymbols)
    //        {
    //            if (symbol.Interfaces.Any(_ => _.Name.Equals("ICommand")))
    //            {
    //                commands.Add(symbol.Name);
    //                continue;
    //            }
    //        }

    //        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

    //        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
    //        {
    //            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
    //            {
    //                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
    //                {
    //                    continue;
    //                }

    //                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
    //                string fullName = attributeContainingTypeSymbol.ToDisplayString();

    //                if (fullName == "SourceGenerators.Common.CQRS.PostRequestAttribute")
    //                {
    //                    return classDeclarationSyntax;
    //                }
    //            }
    //        }

    //        return null;
    //    }

    //    static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> requests, SourceProductionContext context)
    //    {
    //        if (requests.IsDefaultOrEmpty)
    //        {
    //            // nothing to do yet
    //            return;
    //        }
    //    }
}
