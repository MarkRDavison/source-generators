namespace SourceGenerators.Api;

[UseCQRS(typeof(DtoRootType), typeof(CommandsRootType), typeof(QueriesRootType))]
public class ApiRootType
{
}
