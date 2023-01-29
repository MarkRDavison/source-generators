using SourceGenerators.Common;

namespace SourceGenerators.Commands.Example1;

public class Example1CommandHandler : ICommandHandler<Example1CommandRequest, Example1CommandResponse>
{
    public Task<Example1CommandResponse> HandleAsync(Example1CommandRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
