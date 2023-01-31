namespace SourceGenerators.Test;

[TestClass]
public class CQRSEndpointsTests : WebApplicationFactory<Startup>
{
    protected HttpClient Client { get; }

    public CQRSEndpointsTests()
    {
        Client = CreateClient();
    }

    protected async Task<HttpResponseMessage> CallAsync(HttpMethod httpMethod, string uri, object? data)
    {
        HttpRequestMessage httpRequestMessage = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(uri, UriKind.Relative),
            Content = ((data == null) ? null : new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"))
        };

        return await Client.SendAsync(httpRequestMessage);
    }

    protected async Task<T?> GetAsync<T>(string uri, bool requireSuccess = false)
    {
        HttpResponseMessage httpResponseMessage = await CallAsync(HttpMethod.Get, uri, null);
        if (requireSuccess)
        {
            httpResponseMessage.EnsureSuccessStatusCode();
        }

        try
        {
            return await httpResponseMessage.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return default(T);
        }
    }

    protected async Task<T?> PostAsync<T>(string uri, object content, bool requireSuccess = false)
    {
        HttpResponseMessage httpResponseMessage = await CallAsync(HttpMethod.Post, uri, content);
        if (requireSuccess)
        {
            httpResponseMessage.EnsureSuccessStatusCode();
        }

        try
        {
            return await httpResponseMessage.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            return default(T);
        }
    }

    [TestMethod]
    public async Task TestQueryRequestWorks()
    {
        string name = "TestQueryName";
        var response = await GetAsync<TestQueryResponse>(
            $"/api/{TestQueryRequest.Path}?name={name}",
            true);

        Assert.IsNotNull(response);
        Assert.AreEqual(name, response.RequestName);
    }

    [TestMethod]
    public async Task SampleCommandRequestWorks()
    {
        string name = "SampleCommand";

        var response = await PostAsync<SampleCommandResponse>(
            $"/api/{SampleCommandRequest.Path}",
            new SampleCommandRequest { Name = name },
            true);

        Assert.IsNotNull(response);
        Assert.AreEqual(name, response.RequestName);
    }
}
