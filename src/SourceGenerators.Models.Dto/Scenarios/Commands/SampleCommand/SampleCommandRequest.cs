namespace SourceGenerators.Models.Dto.Scenarios.Commands.SampleCommand;

[PostRequest(Path = Path)]
public class SampleCommandRequest : ICommand<SampleCommandRequest, SampleCommandResponse>
{
    public const string Path = "sample-command";
}
