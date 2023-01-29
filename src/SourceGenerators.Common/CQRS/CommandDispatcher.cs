namespace SourceGenerators.Common.CQRS;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommandDispatcher(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Task<TCommandResult> Dispatch<TCommand, TCommandResult>(TCommand command, CancellationToken cancellation)
        where TCommand : class, ICommand<TCommand, TCommandResult>, new()
        where TCommandResult : class, new()
    {
        var handler = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ICommandHandler<TCommand, TCommandResult>>();
        var currentUserContext = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ICurrentUserContext>();
        return handler.HandleAsync(command, currentUserContext, cancellation);
    }
}