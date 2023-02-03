namespace SourceGenerators.Generators.CQRS;

public enum CQRSActivityType
{
    Command,
    Query
}

public class CQRSActivity
{
    public CQRSActivityType Type { get; set; }
    public string Request { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
