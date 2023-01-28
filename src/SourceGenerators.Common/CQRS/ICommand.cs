namespace SourceGenerators.Common.CQRS;

public interface ICommand<TRequest, TResponse>
    where TRequest : class, ICommand<TRequest, TResponse>
    where TResponse : class
{
}
