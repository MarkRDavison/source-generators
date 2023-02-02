namespace SourceGenerators.Common;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UseCQRSAttribute : Attribute
{
    public Type[] Types { get; set; }

    public UseCQRSAttribute(params Type[] types)
    {
        Types = types;
    }
}