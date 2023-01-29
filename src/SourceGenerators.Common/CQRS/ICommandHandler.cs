namespace SourceGenerators.Common.CQRS;

public interface ICommandHandler<TRequest, TResponse>
    where TRequest : class, ICommand<TRequest, TResponse>
    where TResponse : class
{
    Task<TResponse> HandleAsync(TRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken);
}
