namespace SourceGenerators.Common.CQRS;

public interface IQuery<TRequest, TResponse>
    where TRequest : class, IQuery<TRequest, TResponse>
    where TResponse : class
{
}
