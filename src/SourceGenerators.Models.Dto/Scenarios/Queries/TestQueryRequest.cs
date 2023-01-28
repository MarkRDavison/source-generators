namespace SourceGenerators.Models.Dto.Scenarios.Queries;

[GetRequest(Path = "test-get-request")]
public class TestQueryRequest : IQuery<TestQueryRequest, TestQueryResponse>
{
}
