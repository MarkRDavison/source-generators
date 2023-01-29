namespace SourceGenerators.Common.CQRS;

public interface IQueryDispatcher
{
    Task<TQueryResult> Dispatch<TQueryRequest, TQueryResult>(TQueryRequest Query, CancellationToken cancellation)
        where TQueryRequest : class, IQuery<TQueryRequest, TQueryResult>, new()
        where TQueryResult : class, new();
}
