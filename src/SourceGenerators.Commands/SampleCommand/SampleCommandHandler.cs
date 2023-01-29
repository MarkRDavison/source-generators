using SourceGenerators.Common;

namespace SourceGenerators.Commands.SampleCommand;

public class SampleCommandHandler : ICommandHandler<SampleCommandRequest, SampleCommandResponse>
{
    public Task<SampleCommandResponse> HandleAsync(SampleCommandRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
