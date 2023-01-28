namespace SourceGenerators.Models.Dto.Scenarios.Commands.SampleCommand;

[PostRequest(Path = "sample-command")]
public class SampleCommandRequest : ICommand<SampleCommandRequest, SampleCommandResponse>
{
}
