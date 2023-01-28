namespace SourceGenerators.Test;

[TestClass]
public class CQRSGeneratorTests
{
    private readonly IServiceCollection _services;

    public CQRSGeneratorTests()
    {
        _services = new ServiceCollection();
    }

    private void AssertTypeExists<TServiceType>()
    {
        Assert.IsNotNull(_services.FirstOrDefault(_ => _.ServiceType == typeof(ICommandHandler<Example1CommandRequest, Example1CommandResponse>)));
    }

    [TestMethod]
    public void UseCQRS_AddsCommandHandlers()
    {
        _services.UseCQRS();

        AssertTypeExists<ICommandHandler<Example1CommandRequest, Example1CommandResponse>>();
        AssertTypeExists<ICommandHandler<SampleCommandRequest, SampleCommandResponse>>();
    }
}
