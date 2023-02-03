using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGenerators.Generators.CQRS;

[Generator]
public class CQRSGenerator : ISourceGenerator
{

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var (assemblyMarkerClassNamespace, namespaces) = FetchCQRSNamespaces(context);

        if (!namespaces.Any())
        {
            return;
        }

        var symbols = SourceGeneratorHelpers.GetPotentialTypeSymbols(context, namespaces);

        List<CQRSActivity> activities = new();

        var diNamespaces = new HashSet<string>
        {
            "Microsoft.Extensions.DependencyInjection",
            "SourceGenerators.Common.CQRS", // TODO: Replace when transferring to dotnet common
            "SourceGenerators.Common.Service.Helpers" // TODO: Replace when transferring to dotnet common
        };

        foreach (var symbol in symbols)
        {
            var commandActivity = AttemptCreateCommand(symbol, symbols);
            if (commandActivity != null)
            {
                activities.Add(commandActivity);
            }

            var queryActivity = AttemptCreateQuery(symbol, symbols);
            if (queryActivity != null)
            {
                activities.Add(queryActivity);
            }
        }

        if (activities.Any())
        {
            GenerateDepdendencyInjectionExtensions(context, assemblyMarkerClassNamespace, diNamespaces, activities);
            diNamespaces.Add("Microsoft.AspNetCore.Builder");
            GenerateEndpointRouteExtensions(context, assemblyMarkerClassNamespace, diNamespaces, activities);
        }
    }

    private (string, HashSet<string>) FetchCQRSNamespaces(GeneratorExecutionContext context)
    {
        var assemblyMarkerClass = context.Compilation.SourceModule.GlobalNamespace
            .GetNamespaceMembers()
            .SelectMany(SourceGeneratorHelpers.GetAllTypes)
            .FirstOrDefault(_ => _
                .GetAttributes()
                .Any(__ => __.AttributeClass?.Name == "UseCQRSAttribute"));

        if (assemblyMarkerClass == null)
        {
            return (string.Empty, new());
        }

        var at = assemblyMarkerClass.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.Name == "UseCQRSAttribute");

        var assemblyMarkerClassNamespace = SourceGeneratorHelpers.GetNamespace(assemblyMarkerClass);

        var args = at?.ConstructorArguments.FirstOrDefault();
        if (args == null)
        {
            return (string.Empty, new());
        }

        var types = args.Value.Values.Select(_ => _.Value as INamedTypeSymbol).Where(_ => _ != null).Cast<INamedTypeSymbol>().ToList();
        return (assemblyMarkerClassNamespace, new(types.Select(SourceGeneratorHelpers.GetNamespace)));
    }

    private CQRSActivity? AttemptCreateCommand(ITypeSymbol symbol, List<ITypeSymbol> symbols)
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
            var requestFQN = commandInterface.TypeArguments[0].ToDisplayString();
            var responseFQN = commandInterface.TypeArguments[1].ToDisplayString();

            var requestAttribute = symbol.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.Name?.EndsWith("PostRequestAttribute") ?? false);

            return new CQRSActivity
            {
                Type = CQRSActivityType.Command,
                Request = requestFQN,
                Response = responseFQN,
                Handler = commandHandler.ToDisplayString(),
                Path = requestAttribute?.NamedArguments.First(_ => _.Key == "Path").Value.Value as string ?? string.Empty
            };
        }

        return null;
    }

    private CQRSActivity? AttemptCreateQuery(ITypeSymbol symbol, List<ITypeSymbol> symbols)
    {
        var queryInterface = symbol.Interfaces.FirstOrDefault(_ => _.Name.Equals("IQuery") && _.TypeArguments.Length == 2);
        var queryHandler = symbols.FirstOrDefault(_ =>
        {
            return null != _.Interfaces.FirstOrDefault(_ =>
                _.Name.Equals("IQueryHandler") &&
                _.TypeArguments.Length == 2 &&
                _.TypeArguments[0].Name == queryInterface?.TypeArguments[0]?.Name &&
                _.TypeArguments[1].Name == queryInterface?.TypeArguments[1]?.Name);
        });
        if (queryInterface != null && queryHandler != null)
        {
            var requestFQN = queryInterface.TypeArguments[0].ToDisplayString();
            var responseFQN = queryInterface.TypeArguments[1].ToDisplayString();

            var requestAttribute = symbol.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.Name?.EndsWith("GetRequestAttribute") ?? false);

            return new CQRSActivity
            {
                Type = CQRSActivityType.Query,
                Request = requestFQN,
                Response = responseFQN,
                Handler = queryHandler.ToDisplayString(),
                Path = requestAttribute?.NamedArguments.First(_ => _.Key == "Path").Value.Value as string ?? string.Empty
            };
        }
        return null;
    }

    private static void GenerateEndpointRouteExtensions(GeneratorExecutionContext context, string @namespace, HashSet<string> namespaces, List<CQRSActivity> activities)
    {
        StringBuilder stringBuilder = new();
        foreach (var n in namespaces.OrderBy(_ => _))
        {
            stringBuilder.AppendLine($"using {n};");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine($"namespace {@namespace}");
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
                AppendPost(stringBuilder, activity);

            }
            else if (activity.Type == CQRSActivityType.Query)
            {
                AppendGet(stringBuilder, activity);
            }
        }

        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("}");

        context.AddSource("CQRSEndpointRouteBuilderExtensions.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }

    private static void AppendGet(StringBuilder stringBuilder, CQRSActivity activity)
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

    private static void AppendPost(StringBuilder stringBuilder, CQRSActivity activity)
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

    private static void GenerateDepdendencyInjectionExtensions(GeneratorExecutionContext context, string @namespace, HashSet<string> diNamespaces, List<CQRSActivity> activities)
    {
        StringBuilder stringBuilder = new();
        foreach (var n in diNamespaces.OrderBy(_ => _))
        {
            stringBuilder.AppendLine($"using {n};");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine($"namespace {@namespace}");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    public static class CQRSDependecyInjectionExtensions");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("        public static void UseCQRS(this IServiceCollection services)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine(string.Empty);

        foreach (var command in activities.Where(_ => _.Type == CQRSActivityType.Command)) 
        {
            stringBuilder.AppendLine($"            services.AddScoped<ICommandHandler<{command.Request},{command.Response}>, {command.Handler}>();");
        }
        foreach (var query in activities.Where(_ => _.Type == CQRSActivityType.Query)) 
        {
            stringBuilder.AppendLine($"            services.AddScoped<IQueryHandler<{query.Request},{query.Response}>, {query.Handler}>();");
        }

        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine(string.Empty);
        stringBuilder.AppendLine("}");

        context.AddSource("CQRSDependecyInjectionExtensions.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }
}
