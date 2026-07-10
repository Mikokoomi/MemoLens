using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MemoLens.Tests;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_ReturnsSuccess()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsSuccessJson()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/health");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("MemoLens", document.RootElement.GetProperty("data").GetProperty("appName").GetString());
    }

    [Fact]
    public async Task Memories_GuestRedirectsToLogin()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        var response = await client.GetAsync("/Memories");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task AccountMe_WithoutToken_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient(bool allowAutoRedirect = true)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });

        client.BaseAddress = new Uri("https://localhost");
        return client;
    }
}
