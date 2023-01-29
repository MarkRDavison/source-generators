namespace SourceGenerators.Common.CQRS;

public interface ICommandDispatcher
{
    Task<TCommandResult> Dispatch<TCommandRequest, TCommandResult>(TCommandRequest command, CancellationToken cancellation)
        where TCommandRequest : class, ICommand<TCommandRequest, TCommandResult>, new()
        where TCommandResult : class, new();
}