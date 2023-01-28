namespace SourceGenerators.Commands.SampleCommand;

public class SampleCommandHandler : ICommandHandler<SampleCommandRequest, SampleCommandResponse>
{
    public Task<SampleCommandResponse> HandleAsync(SampleCommandRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
