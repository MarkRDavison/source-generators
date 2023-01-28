namespace SourceGenerators.Common.CQRS;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GetRequestAttribute : Attribute
{
    public string Path { get; set; } = string.Empty;
}
