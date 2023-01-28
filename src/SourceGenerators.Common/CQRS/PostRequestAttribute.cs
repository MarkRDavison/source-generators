namespace SourceGenerators.Common.CQRS;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PostRequestAttribute : Attribute
{
    public string Path { get; set; } = string.Empty;
}
