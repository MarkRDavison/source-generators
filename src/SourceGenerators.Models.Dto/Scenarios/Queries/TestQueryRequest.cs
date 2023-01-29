namespace SourceGenerators.Models.Dto.Scenarios.Queries;

[GetRequest(Path = Path)]
public class TestQueryRequest : IQuery<TestQueryRequest, TestQueryResponse>
{
    public const string Path = "test-get-request";
}
