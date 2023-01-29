using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace SourceGenerators.Test;


[TestClass]
public class CQRSGeneratorTests
{
    private class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = null!;
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder()
        {
            throw new NotImplementedException();
        }
    }

    private readonly IServiceCollection _services;
    private readonly IEndpointRouteBuilder _endpoints;

    public CQRSGeneratorTests()
    {
        _services = new ServiceCollection();
        _endpoints = new TestEndpointRouteBuilder();
    }

    private void AssertTypeExists<TServiceType>()
    {
        Assert.IsNotNull(_services.FirstOrDefault(_ => _.ServiceType == typeof(TServiceType)));
    }

    [TestMethod]
    public void UseCQRS_AddsCommandHandlers()
    {
        _services.UseCQRS();

        AssertTypeExists<ICommandHandler<Example1CommandRequest, Example1CommandResponse>>();
        AssertTypeExists<ICommandHandler<SampleCommandRequest, SampleCommandResponse>>();
    }

    [TestMethod]
    public void UseCQRS_AddsQueryHandlers()
    {
        _services.UseCQRS();

        AssertTypeExists<IQueryHandler<TestQueryRequest, TestQueryResponse>>();
    }

    [TestMethod]
    public void ConfigureCQRSEndpoints_AddsCommandRoutes()
    {
        _endpoints.ConfigureCQRSEndpoints();

        Assert.IsNotNull(_endpoints.DataSources
            .FirstOrDefault(_ => _.Endpoints
            .Any(__ => __.DisplayName == $"HTTP: POST /api/{Example1CommandRequest.Path}")));

        Assert.IsNotNull(_endpoints.DataSources
            .FirstOrDefault(_ => _.Endpoints
            .Any(__ => __.DisplayName == $"HTTP: POST /api/{SampleCommandRequest.Path}")));
    }

    [TestMethod]
    public void ConfigureCQRSEndpoints_AddsQueryRoutes()
    {
        _endpoints.ConfigureCQRSEndpoints();

        Assert.IsNotNull(_endpoints.DataSources
            .FirstOrDefault(_ => _.Endpoints
            .Any(__ => __.DisplayName == $"HTTP: GET /api/{TestQueryRequest.Path}")));
    }
}
