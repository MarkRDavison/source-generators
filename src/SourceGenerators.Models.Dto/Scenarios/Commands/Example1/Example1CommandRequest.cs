namespace SourceGenerators.Models.Dto.Scenarios.Commands.Example1;

[PostRequest(Path = "example1-command")]
public class Example1CommandRequest : ICommand<Example1CommandRequest, Example1CommandResponse>
{
    public int Integer { get; set; }
}
