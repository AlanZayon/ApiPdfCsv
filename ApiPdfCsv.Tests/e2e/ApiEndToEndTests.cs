using ApiPdfCsv.Tests;
using Xunit;

namespace ApiPdfCsv.Tests.E2e;

public class ApiEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiEndToEndTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTestEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var contentType = response.Content?.Headers.ContentType?.ToString();
        Assert.Equal("text/plain; charset=utf-8", contentType);
    }
}
