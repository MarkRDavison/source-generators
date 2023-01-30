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
