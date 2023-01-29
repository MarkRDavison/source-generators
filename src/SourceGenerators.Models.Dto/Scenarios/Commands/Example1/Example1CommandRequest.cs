namespace SourceGenerators.Models.Dto.Scenarios.Commands.Example1;

[PostRequest(Path = Path)]
public class Example1CommandRequest : ICommand<Example1CommandRequest, Example1CommandResponse>
{
    public const string Path = "example1-command";
    public int Integer { get; set; }
}
