namespace SourceGenerators.Queries.TestQuery;

public class TestQueryQueryHandler : IQueryHandler<TestQueryRequest, TestQueryResponse>
{
    public Task<TestQueryResponse> HandleAsync(TestQueryRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
