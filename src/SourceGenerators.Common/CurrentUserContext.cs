namespace SourceGenerators.Common;

public class CurrentUserContext : ICurrentUserContext
{
    public string Token { get; set; } = default!;
}