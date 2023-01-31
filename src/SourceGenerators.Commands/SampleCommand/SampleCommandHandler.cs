namespace SourceGenerators.Commands.SampleCommand;

public class SampleCommandHandler : ICommandHandler<SampleCommandRequest, SampleCommandResponse>
{
    public Task<SampleCommandResponse> HandleAsync(SampleCommandRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SampleCommandResponse
        {
            RequestName = request.Name
        });
    }
}
