using System.Text;

namespace SourceGenerators.Generators
{
    public static class SourceGenerationHelper
    {

        public const string UseCQRSAttribute = @"
namespace SourceGenerators.Generators;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UseCQRSAttribute : Attribute
{
    public Type[] Types { get; set; }

    public UseCQRSAttribute(params Type[] types) {
       this.Types = types;
    }
}
";

        // TODO: Move this to common server helper assembly?
        public static string ActivityHandlerHelpers(string ns)
        {
            return $@"using SourceGenerators.Common.CQRS;
using System.Text.Json;

namespace {ns}
{{
    public static class CQRSHelpers
    {{

        public static async Task<string> GetRequestBody(HttpRequest request)
        {{
            try
            {{
                if (request.ContentLength == 0) {{ return string.Empty; }}
                var bodyStream = new StreamReader(request.Body);

                var bodyText = await bodyStream.ReadToEndAsync();
                return bodyText;
            }}
            catch (Exception)
            {{
                return string.Empty;
            }}
        }}

        public static TRequest GetRequestFromQuery<TRequest, TResponse>(HttpRequest httpRequest)
            where TRequest : class, IQuery<TRequest, TResponse>, new()
            where TResponse : class, new()
        {{

            var requestProperties = typeof(TRequest)
                .GetProperties()
                .Where(_ => _.CanWrite)
                .ToList();

            var request = new TRequest();

            var queryDictionary = httpRequest.Query.ToDictionary(_ => _.Key, _ => _.Value.ToString());
            foreach (var property in requestProperties.Where(_ => queryDictionary.ContainsKey(_.Name.ToLowerInvariant())))
            {{
                var queryProperty = queryDictionary[property.Name.ToLowerInvariant()];

                if (property.PropertyType == typeof(string))
                {{
                    property.SetValue(request, queryProperty);
                }}
                else if (property.PropertyType == typeof(Guid))
                {{
                    if (Guid.TryParse(queryProperty, out var guid))
                    {{
                        property.SetValue(request, guid);
                    }}
                    else
                    {{
                        throw new InvalidOperationException(""Invalid Guid format"");
                    }}
                }}
                else if (property.PropertyType == typeof(long))
                {{
                    if (long.TryParse(queryProperty, out var lnum))
                    {{
                        property.SetValue(request, lnum);
                    }}
                    else
                    {{
                        throw new InvalidOperationException(""Invalid long format"");
                    }}
                }}
                else if (property.PropertyType == typeof(int))
                {{
                    if (int.TryParse(queryProperty, out var lnum))
                    {{
                        property.SetValue(request, lnum);
                    }}
                    else
                    {{
                        throw new InvalidOperationException(""Invalid int format"");
                    }}
                }}
                else if (property.PropertyType == typeof(bool))
                {{
                    if (bool.TryParse(queryProperty, out var bval))
                    {{
                        property.SetValue(request, bval);
                    }}
                    else
                    {{
                        throw new InvalidOperationException(""Invalid bool format"");
                    }}
                }}
                else if (property.PropertyType == typeof(DateOnly))
                {{
                    if (DateOnly.TryParse(queryProperty, out var dval))
                    {{
                        property.SetValue(request, dval);
                    }}
                    else
                    {{
                        throw new InvalidOperationException(""Invalid DateOnly format"");
                    }}
                }}
                else
                {{
                    throw new InvalidOperationException($""Unhandled get property type {{property.PropertyType.Name}}"");
                }}
            }}

            return request;
        }}

        public static async Task<TRequest> GetRequestFromBody<TRequest, TResponse>(HttpRequest httpRequest)
            where TRequest : class, ICommand<TRequest, TResponse>, new()
            where TResponse : class, new()
        {{
            var bodyText = await GetRequestBody(httpRequest);
            if (string.IsNullOrEmpty(bodyText))
            {{
                return new TRequest();
            }}
            var request = JsonSerializer.Deserialize<TRequest>(bodyText); // SerializationHelpers.CreateStandardSerializationOptions()
            if (request == null)
            {{
                return new TRequest();
            }}

            return request;            
        }}
    }}
}}";
        }

        public static string CQRSExtensions(string ns)
        {
            return @$"namespace {ns}
{{
    public static class CQRSDependencyInjectionExtensions {{

        public static void UseCQRS(this IServiceCollection services)
        {{

        }}

    }}
}}";
        }

        public const string Attribute = @"
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

}
