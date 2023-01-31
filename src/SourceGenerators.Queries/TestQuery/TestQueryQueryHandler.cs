namespace SourceGenerators.Queries.TestQuery;

public class TestQueryQueryHandler : IQueryHandler<TestQueryRequest, TestQueryResponse>
{
    public Task<TestQueryResponse> HandleAsync(TestQueryRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestQueryResponse
        {
            RequestName = request.Name
        });
    }
}
