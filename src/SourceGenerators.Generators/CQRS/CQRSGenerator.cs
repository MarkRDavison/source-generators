using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGenerators.Generators.CQRS;

[Generator]
public class CQRSGenerator : ISourceGenerator
{
    static string GetNamespace(ITypeSymbol syntax)
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

    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("UseCQRSAttribute.g.cs", SourceText.From(SourceGenerationHelper.UseCQRSAttribute, Encoding.UTF8));

        var assemblyMarkerAttribute = context.Compilation.SourceModule.GlobalNamespace
            .GetNamespaceMembers()
            .SelectMany(GetAllTypes)
            .FirstOrDefault(_ => _
                .GetAttributes()
                .Any(__ => __.AttributeClass?.Name == "UseCQRS"));

        if (assemblyMarkerAttribute == null)
        {
            return;
        }

        var symbols = context.Compilation.SourceModule.ReferencedAssemblySymbols
            .Where(_ => _.Identity.Name.StartsWith("SourceGenerators")) // TODO: Replace with getting the assemblies we are interested in from the UseCQRS attribute
            .SelectMany(_ =>
            {
                try
                {
                    var main = _.Identity.Name.Split('.').Aggregate(_.GlobalNamespace, (s, c) => s.GetNamespaceMembers().Single(m => m.Name.Equals(c)));

                    return GetAllTypes(main);
                }
                catch
                {
                    return Enumerable.Empty<ITypeSymbol>();
                }
            })
            .ToList();

        List<CQRSActivity> activities = new();

        bool present = false;
        var diNamespaces = new HashSet<string>();
        var commandHandlers = new HashSet<string>();
        var queryHandlers = new HashSet<string>();
        diNamespaces.Add("Microsoft.Extensions.DependencyInjection");
        diNamespaces.Add("SourceGenerators.Common.CQRS"); // TODO: Replace when transferring to dotnet common
        diNamespaces.Add("SourceGenerators.Common.Service.Helpers"); // TODO: Replace when transferring to dotnet common

        foreach (var symbol in symbols)
        {
            var commandInterface = symbol.Interfaces.FirstOrDefault(_ => _.Name.Equals("ICommand") && _.TypeArguments.Length == 2);
            var queryInterface = symbol.Interfaces.FirstOrDefault(_ => _.Name.Equals("IQuery") && _.TypeArguments.Length == 2);
            var commandHandler = symbols.FirstOrDefault(_ =>
            {
                return null != _.Interfaces.FirstOrDefault(_ =>
                    _.Name.Equals("ICommandHandler") &&
                    _.TypeArguments.Length == 2 &&
                    _.TypeArguments[0].Name == commandInterface?.TypeArguments[0]?.Name &&
                    _.TypeArguments[1].Name == commandInterface?.TypeArguments[1]?.Name);
            });
            var queryHandler = symbols.FirstOrDefault(_ =>
            {
                return null != _.Interfaces.FirstOrDefault(_ =>
                    _.Name.Equals("IQueryHandler") &&
                    _.TypeArguments.Length == 2 &&
                    _.TypeArguments[0].Name == queryInterface?.TypeArguments[0]?.Name &&
                    _.TypeArguments[1].Name == queryInterface?.TypeArguments[1]?.Name);
            });
            if (commandInterface != null && commandHandler != null)
            {
                present = true;
                commandHandlers.Add($"services.AddScoped<ICommandHandler<{commandInterface.TypeArguments[0].Name},{commandInterface.TypeArguments[1].Name}>, {commandHandler.Name}>()");
                diNamespaces.Add(GetNamespace(commandHandler));
                diNamespaces.Add(GetNamespace(commandInterface.TypeArguments[0]));
                diNamespaces.Add(GetNamespace(commandInterface.TypeArguments[1]));

                var requestAttribute = symbol.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.Name?.EndsWith("PostRequestAttribute") ?? false);

                activities.Add(new CQRSActivity
                {
                    Type = CQRSActivityType.Command,
                    Request = commandInterface.TypeArguments[0].Name, // TODO: Fully qualified?
                    Response = commandInterface.TypeArguments[1].Name, // TODO: Fully qualified?
                    Path = requestAttribute?.NamedArguments.First(_ => _.Key == "Path").Value.Value as string ?? string.Empty
                });
            }
            if (queryInterface != null && queryHandler != null)
            {
                present = true;
                commandHandlers.Add($"services.AddScoped<IQueryHandler<{queryInterface.TypeArguments[0].Name},{queryInterface.TypeArguments[1].Name}>, {queryHandler.Name}>()");
                diNamespaces.Add(GetNamespace(queryHandler));
                diNamespaces.Add(GetNamespace(queryInterface.TypeArguments[0]));
                diNamespaces.Add(GetNamespace(queryInterface.TypeArguments[1]));

                var requestAttribute = symbol.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.Name?.EndsWith("GetRequestAttribute") ?? false);

                activities.Add(new CQRSActivity
                {
                    Type = CQRSActivityType.Query,
                    Request = queryInterface.TypeArguments[0].Name, // TODO: Fully qualified?
                    Response = queryInterface.TypeArguments[1].Name, // TODO: Fully qualified?
                    Path = requestAttribute?.NamedArguments.First(_ => _.Key == "Path").Value.Value as string ?? string.Empty
                });
            }
        }

        if (present)
        {
            GenerateDepdendencyInjectionExtensions(context, diNamespaces, commandHandlers, queryHandlers);
            diNamespaces.Add("Microsoft.AspNetCore.Builder");
            GenerateEndpointRouteExtensions(context, diNamespaces, activities);
        }
    }

    private static void GenerateEndpointRouteExtensions(GeneratorExecutionContext context, HashSet<string> namespaces, List<CQRSActivity> activities)
    {
        StringBuilder stringBuilder = new();
        foreach (var n in namespaces.OrderBy(_ => _))
        {
            stringBuilder.AppendLine($"using {n};");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("namespace SourceGenerators.Test");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    public static class GenerateEndpointRouteExtensions");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("        public static void ConfigureCQRSEndpoints(this IEndpointRouteBuilder endpoints)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine(string.Empty);

        foreach (var activity in activities.Where(_ => !string.IsNullOrEmpty(_.Path)))
        {
            if (activity.Type == CQRSActivityType.Command)
            {
                stringBuilder.AppendLine("            endpoints.MapPost(");
                stringBuilder.AppendLine($"                \"/api/{activity.Path}\",");
                stringBuilder.AppendLine("                async (HttpContext context, CancellationToken cancellationToken) =>");
                stringBuilder.AppendLine("                {");
                stringBuilder.AppendLine("                    var dispatcher = context.RequestServices.GetRequiredService<ICommandDispatcher>();");
                stringBuilder.AppendLine($"                    var request = await RequestHelpers.GetRequestFromBody<{activity.Request},{activity.Response}>(context.Request);");
                stringBuilder.AppendLine($"                    return await dispatcher.Dispatch<{activity.Request},{activity.Response}>(request, cancellationToken);");
                stringBuilder.AppendLine("                });");
                stringBuilder.AppendLine(string.Empty);

            }
            else if (activity.Type == CQRSActivityType.Query)
            {
                stringBuilder.AppendLine("            endpoints.MapGet(");
                stringBuilder.AppendLine($"                \"/api/{activity.Path}\",");
                stringBuilder.AppendLine("                async (HttpContext context, CancellationToken cancellationToken) =>");
                stringBuilder.AppendLine("                {");
                stringBuilder.AppendLine("                    var dispatcher = context.RequestServices.GetRequiredService<IQueryDispatcher>();");
                stringBuilder.AppendLine($"                    var request = RequestHelpers.GetRequestFromQuery<{activity.Request},{activity.Response}>(context.Request);");
                stringBuilder.AppendLine($"                    return await dispatcher.Dispatch<{activity.Request},{activity.Response}>(request, cancellationToken);");
                stringBuilder.AppendLine("                });");
                stringBuilder.AppendLine(string.Empty);
            }
        }

        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("}");

        context.AddSource("CQRSEndpointRouteBuilderExtensions.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }
    private static void GenerateDepdendencyInjectionExtensions(GeneratorExecutionContext context, HashSet<string> diNamespaces, HashSet<string> commandHandlers, HashSet<string> queryHandlers)
    {
        StringBuilder stringBuilder = new();
        foreach (var n in diNamespaces.OrderBy(_ => _))
        {
            stringBuilder.AppendLine($"using {n};");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("namespace SourceGenerators.Test");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    public static class CQRSDependecyInjectionExtensions");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("        public static void UseCQRS(this IServiceCollection services)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine(string.Empty);

        foreach (var handler in commandHandlers.Concat(queryHandlers).OrderBy(_ => _))
        {
            stringBuilder.AppendLine($"            {handler};");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("}");

        context.AddSource("CQRSDependecyInjectionExtensions.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
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
}
