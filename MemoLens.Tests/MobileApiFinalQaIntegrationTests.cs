using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Auth;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MemoLens.Tests;

public class MobileApiFinalQaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "MemoLens1";
    private const string InvalidSigningSecret = "MemoLens-Invalid-Signing-Key-At-Least-32-Bytes";

    private readonly CustomWebApplicationFactory _factory;

    public MobileApiFinalQaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedApi_InvalidExpiredAndMissingClaimTokens_ReturnStableJson401()
    {
        var user = await CreateUserAsync("Bearer Matrix");
        var tokens = new[]
        {
            "not-a-jwt",
            _factory.CreateTestAccessToken(
                user.Id,
                DateTime.UtcNow.AddMinutes(5),
                InvalidSigningSecret,
                user.Email,
                [IdentitySeedData.UserRole]),
            _factory.CreateTestAccessToken(
                user.Id,
                DateTime.UtcNow.AddMinutes(-1),
                email: user.Email,
                roles: [IdentitySeedData.UserRole]),
            _factory.CreateTestAccessToken(
                null,
                DateTime.UtcNow.AddMinutes(5),
                email: user.Email,
                roles: [IdentitySeedData.UserRole])
        };

        foreach (var token in tokens)
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync("/api/v1/account/me");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            AssertJsonAndNoImplementationDetails(response, body);
            using var json = JsonDocument.Parse(body);
            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        }
    }

    [Fact]
    public async Task MvcCookie_DoesNotAuthorizeBearerApi_AndMvcSessionStillWorks()
    {
        var user = await CreateUserAsync("Cookie Boundary");
        using var client = await CreateCookieClientAsync(user);

        using var mvcResponse = await client.GetAsync("/Memories");
        Assert.Equal(HttpStatusCode.OK, mvcResponse.StatusCode);

        using var apiResponse = await client.GetAsync("/api/v1/memories");
        var body = await apiResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, apiResponse.StatusCode);
        AssertJsonAndNoImplementationDetails(apiResponse, body);
    }

    [Fact]
    public async Task ApiFailures_UseStableStatuses_AndNeverReturnHtmlOrStackTraces()
    {
        var user = await CreateUserAsync("Contract QA");
        using var client = await CreateBearerClientAsync(user);

        var requests = new[]
        {
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/memories")
            {
                Content = new StringContent("{", Encoding.UTF8, "application/json")
            },
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/memories")
            {
                Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
            },
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/memories/1")
            {
                Content = JsonContent.Create(ValidMemoryRequest())
            },
            new HttpRequestMessage(HttpMethod.Get, "/api/v1/memories/not-an-id"),
            new HttpRequestMessage(HttpMethod.Get, "/api/v1/memories?from=not-a-date")
        };
        var expectedStatuses = new[]
        {
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest
        };

        for (var index = 0; index < requests.Length; index++)
        {
            using var response = await client.SendAsync(requests[index]);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedStatuses[index], response.StatusCode);
            AssertNoImplementationDetails(body);
            Assert.DoesNotContain("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AlbumList_ExtremePageNumber_IsNormalizedWithoutServerError()
    {
        var user = await CreateUserAsync("Large Page");
        using var client = await CreateBearerClientAsync(user);

        using var createResponse = await client.PostAsJsonAsync("/api/v1/albums", new
        {
            title = "Album kiểm tra phân trang",
            description = "Album phải không xuất hiện ở trang cực lớn."
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var albumId = createdJson.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var response = await client.GetAsync($"/api/v1/albums?page={int.MaxValue}&pageSize=50");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJsonAndNoImplementationDetails(response, body);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("items").EnumerateArray());

        using var detailsResponse = await client.GetAsync($"/api/v1/albums/{albumId}?page={int.MaxValue}&pageSize=50");
        using var detailsJson = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        Assert.Empty(detailsJson.RootElement
            .GetProperty("data")
            .GetProperty("memories")
            .GetProperty("items")
            .EnumerateArray());
    }

    [Fact]
    public async Task MemoryList_PageSizeOneSecondPage_DoesNotWrapBackToFirstPage()
    {
        var user = await CreateUserAsync("Memory Page Boundary");
        using var client = await CreateBearerClientAsync(user);

        using var createResponse = await client.PostAsJsonAsync("/api/v1/memories", ValidMemoryRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var firstPageResponse = await client.GetAsync("/api/v1/memories?page=1&pageSize=1");
        using var firstPageJson = JsonDocument.Parse(await firstPageResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        Assert.Single(firstPageJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray());

        using var response = await client.GetAsync("/api/v1/memories?page=2&pageSize=1");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task ExpiredRefreshToken_IsRejectedWithoutLeakingStoredHash()
    {
        var user = await CreateUserAsync("Expired Refresh");
        string rawToken;
        string tokenHash;

        using (var scope = _factory.Services.CreateScope())
        {
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            rawToken = tokenService.GenerateRefreshToken();
            tokenHash = tokenService.HashRefreshToken(rawToken);
            dbContext.UserRefreshTokens.Add(new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rawToken });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertJsonAndNoImplementationDetails(response, body);
        Assert.DoesNotContain(rawToken, body, StringComparison.Ordinal);
        Assert.DoesNotContain(tokenHash, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProtectedResponses_DoNotExposeOwnershipSecretsOrStoragePaths()
    {
        var user = await CreateUserAsync("Safe Response");
        using var client = await CreateBearerClientAsync(user);

        using var memoryResponse = await client.PostAsJsonAsync("/api/v1/memories", ValidMemoryRequest());
        using var albumResponse = await client.PostAsJsonAsync("/api/v1/albums", new
        {
            title = "Bộ sưu tập an toàn",
            description = "Chỉ trả dữ liệu dành cho mobile."
        });
        using var accountResponse = await client.GetAsync("/api/v1/account/me");

        Assert.Equal(HttpStatusCode.Created, memoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, albumResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accountResponse.StatusCode);

        var combinedBody = string.Join('\n',
            await memoryResponse.Content.ReadAsStringAsync(),
            await albumResponse.Content.ReadAsStringAsync(),
            await accountResponse.Content.ReadAsStringAsync());

        foreach (var forbiddenValue in new[]
                 {
                     "userId", "passwordHash", "securityStamp", "tokenHash",
                     "replacedByTokenHash", "imagePath", "App_Data", "uploads/"
                 })
        {
            Assert.DoesNotContain(forbiddenValue, combinedBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Swagger_IsDevelopmentOnly_AndDocumentsMobileApiContracts()
    {
        using (var testingClient = CreateClient())
        using (var testingResponse = await testingClient.GetAsync("/swagger/v1/swagger.json"))
        {
            Assert.Equal(HttpStatusCode.NotFound, testingResponse.StatusCode);
        }

        using var developmentFactory = new DevelopmentWebApplicationFactory();
        using var developmentClient = developmentFactory.CreateClient();
        developmentClient.BaseAddress = new Uri("https://localhost");
        using var swaggerResponse = await developmentClient.GetAsync("/swagger/v1/swagger.json");
        var body = await swaggerResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
        using var swagger = JsonDocument.Parse(body);
        var root = swagger.RootElement;
        Assert.Equal("MemoLens API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());

        var paths = root.GetProperty("paths");
        foreach (var path in new[]
                 {
                     "/api/v1/health",
                     "/api/v1/auth/login",
                     "/api/v1/account/me",
                     "/api/v1/memories",
                     "/api/v1/memories/{id}",
                     "/api/v1/memories/{memoryId}/images",
                     "/api/v1/images/{imageId}/content",
                     "/api/v1/albums",
                     "/api/v1/albums/{id}/memories"
                 })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"Swagger is missing {path}.");
        }

        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());

        var uploadContent = paths
            .GetProperty("/api/v1/memories/{memoryId}/images")
            .GetProperty("post")
            .GetProperty("requestBody")
            .GetProperty("content");
        Assert.True(uploadContent.TryGetProperty("multipart/form-data", out _));
    }

    private async Task<TestUser> CreateUserAsync(string displayName)
    {
        var email = $"mobile-final-qa-{Guid.NewGuid():N}@example.test";
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, IdentitySeedData.UserRole)).Succeeded);
        return new TestUser(user.Id, email);
    }

    private async Task<HttpClient> CreateBearerClientAsync(TestUser user)
    {
        var client = CreateClient();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var applicationUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(applicationUser);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await tokenService.GenerateAccessTokenAsync(applicationUser));
        return client;
    }

    private async Task<HttpClient> CreateCookieClientAsync(TestUser user)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.BaseAddress = new Uri("https://localhost");

        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(
            loginPage,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")
            .Groups["token"]
            .Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("__RequestVerificationToken", WebUtility.HtmlDecode(token)),
            new KeyValuePair<string, string>("Email", user.Email),
            new KeyValuePair<string, string>("Password", Password),
            new KeyValuePair<string, string>("RememberMe", "false")
        ]));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        return client;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static object ValidMemoryRequest() => new
    {
        title = "Kỷ niệm kiểm thử cuối",
        story = "Nội dung không chứa dữ liệu nội bộ.",
        feeling = "Bình yên",
        memoryDate = "2026-07-14",
        location = "Hà Nội",
        tags = new[] { "qa", "mobile" }
    };

    private static void AssertJsonAndNoImplementationDetails(HttpResponseMessage response, string body)
    {
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        AssertNoImplementationDetails(body);
    }

    private static void AssertNoImplementationDetails(string body)
    {
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at MemoLens.", body, StringComparison.Ordinal);
    }

    private sealed class DevelopmentWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override string TestEnvironmentName => Environments.Development;
    }

    private sealed record TestUser(string Id, string Email);
}
