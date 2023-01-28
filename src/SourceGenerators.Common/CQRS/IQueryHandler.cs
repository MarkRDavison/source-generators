namespace SourceGenerators.Common.CQRS;

public interface IQueryHandler<TRequest, TResponse>
    where TRequest : class, IQuery<TRequest, TResponse>
    where TResponse : class
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
