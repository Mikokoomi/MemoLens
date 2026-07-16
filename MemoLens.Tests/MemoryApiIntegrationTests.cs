using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Services;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoLens.Tests;

public class MemoryApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Password = "MemoLens1";
    private readonly CustomWebApplicationFactory _factory;

    public MemoryApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _factory.ClearTestUploadStorage();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.ClearTestUploadStorage();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("GET", "/api/v1/memories")]
    [InlineData("GET", "/api/v1/memories/999999")]
    [InlineData("POST", "/api/v1/memories")]
    [InlineData("PUT", "/api/v1/memories/999999")]
    [InlineData("PUT", "/api/v1/memories/999999/cover")]
    [InlineData("DELETE", "/api/v1/memories/999999/cover")]
    [InlineData("DELETE", "/api/v1/memories/999999")]
    [InlineData("POST", "/api/v1/memories/999999/restore")]
    public async Task AllMemoryEndpoints_WithoutBearerToken_ReturnUnauthorized(string method, string path)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(ValidRequest()) : null
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("success", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CoverOverride_UsesManualImageAndResetReturnsUploadOrder()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Cover memory", "Bình yên", DateTime.UtcNow.Date, null, []);
        var firstImage = await CreateImageAsync(memory);
        var secondImage = await CreateImageAsync(memory);
        using var client = await CreateBearerClientAsync(owner);

        using var setResponse = await client.PutAsJsonAsync($"/api/v1/memories/{memory.Id}/cover", new { imageId = secondImage.Id });
        using var setDocument = JsonDocument.Parse(await setResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        Assert.Equal(secondImage.Id, setDocument.RootElement.GetProperty("data").GetProperty("manualCoverImageId").GetInt32());
        Assert.Equal(secondImage.Id, setDocument.RootElement.GetProperty("data").GetProperty("effectiveCoverImageId").GetInt32());

        using var resetResponse = await client.DeleteAsync($"/api/v1/memories/{memory.Id}/cover");
        using var resetDocument = JsonDocument.Parse(await resetResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(JsonValueKind.Null, resetDocument.RootElement.GetProperty("data").GetProperty("manualCoverImageId").ValueKind);
        Assert.Equal(firstImage.Id, resetDocument.RootElement.GetProperty("data").GetProperty("effectiveCoverImageId").GetInt32());
    }

    [Fact]
    public async Task Create_UsesCurrentUserAndNormalizesTags_WithoutPrivateFields()
    {
        var owner = await CreateUserAsync("Owner");
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.PostAsJsonAsync("/api/v1/memories", new
        {
            title = "  Buổi chiều yên bình  ",
            story = "  Một câu chuyện riêng.  ",
            feeling = "Bình yên",
            memoryDate = "2026-07-01",
            location = "  Hà Nội  ",
            tags = new[] { " Du lịch ", "du lịch", "", "  Cà phê" }
        });
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("Buổi chiều yên bình", data.GetProperty("title").GetString());
        Assert.Equal(2, data.GetProperty("tags").GetArrayLength());
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("imagePath", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);

        var memoryId = data.GetProperty("id").GetInt32();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = await dbContext.Memories.Include(item => item.MemoryTags)
            .SingleAsync(item => item.Id == memoryId);
        Assert.Equal(owner.Id, memory.UserId);
        Assert.Equal("Hà Nội", memory.Location);
        Assert.Equal(2, memory.MemoryTags.Count);
    }

    [Theory]
    [InlineData("missing-title")]
    [InlineData("invalid-feeling")]
    [InlineData("missing-date")]
    public async Task Create_InvalidData_ReturnsVietnameseValidationErrors(string caseName)
    {
        var owner = await CreateUserAsync("Owner");
        using var client = await CreateBearerClientAsync(owner);
        object body;
        switch (caseName)
        {
            case "missing-title":
                body = new { title = "", feeling = "Bình yên", memoryDate = "2026-07-01" };
                break;
            case "invalid-feeling":
                body = new { title = "Hợp lệ", feeling = "Phấn khích", memoryDate = "2026-07-01" };
                break;
            default:
                body = new { title = "Hợp lệ", feeling = "Bình yên", memoryDate = (string?)null };
                break;
        }

        using var response = await client.PostAsJsonAsync("/api/v1/memories", body);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            responseBody.Contains("Tiêu đề", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("Cảm xúc", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("Ngày", StringComparison.OrdinalIgnoreCase),
            "Phản hồi xác thực phải chứa thông điệp tiếng Việt có dấu.");
    }

    [Fact]
    public async Task Create_MalformedJson_ReturnsBadRequestWithoutImplementationDetails()
    {
        var owner = await CreateUserAsync("Owner");
        using var client = await CreateBearerClientAsync(owner);
        using var response = await client.PostAsync(
            "/api/v1/memories",
            new StringContent("{ not-json", System.Text.Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sql", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_IsOwnerScopedForUsersAndAdmin_AndSupportsFiltersAndSort()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        await CreateMemoryAsync(owner, "Đà Lạt", "Bình yên", new DateTime(2026, 7, 2), "Đà Lạt", ["travel"]);
        await CreateMemoryAsync(owner, "Cà phê", "Vui vẻ", new DateTime(2026, 6, 5), "Hà Nội", ["coffee"]);
        var privateMemory = await CreateMemoryAsync(other, "Không thuộc Owner", "Buồn", new DateTime(2026, 7, 4), "Huế", ["private"]);

        using var ownerClient = await CreateBearerClientAsync(owner);
        using var adminClient = await CreateBearerClientAsync(admin);
        using var response = await ownerClient.GetAsync("/api/v1/memories?search=Đà&feeling=Bình%20yên&tag=travel&year=2026&month=7&sort=oldest");
        var ownerBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(ownerBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = document.RootElement.GetProperty("data").GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("Đà Lạt", items[0].GetProperty("title").GetString());
        Assert.DoesNotContain(privateMemory.Title, ownerBody);

        using var adminResponse = await adminClient.GetAsync($"/api/v1/memories/{privateMemory.Id}");
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    [Fact]
    public async Task List_UsesDatabasePaginationAndNormalizesPageSize()
    {
        var owner = await CreateUserAsync("Owner");
        for (var day = 1; day <= 5; day++)
        {
            await CreateMemoryAsync(owner, $"Kỷ niệm {day}", "Bình yên", new DateTime(2026, 7, day), null, []);
        }

        using var client = await CreateBearerClientAsync(owner);
        using var response = await client.GetAsync("/api/v1/memories?page=2&pageSize=2&sort=oldest");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, data.GetProperty("page").GetInt32());
        Assert.Equal(2, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(5, data.GetProperty("totalItems").GetInt32());
        Assert.Equal(3, data.GetProperty("totalPages").GetInt32());
        Assert.True(data.GetProperty("hasPreviousPage").GetBoolean());
        Assert.True(data.GetProperty("hasNextPage").GetBoolean());
        Assert.Equal("Kỷ niệm 3", data.GetProperty("items")[0].GetProperty("title").GetString());

        using var cappedResponse = await client.GetAsync("/api/v1/memories?page=0&pageSize=999");
        using var cappedDocument = JsonDocument.Parse(await cappedResponse.Content.ReadAsStringAsync());
        var cappedData = cappedDocument.RootElement.GetProperty("data");
        Assert.Equal(1, cappedData.GetProperty("page").GetInt32());
        Assert.Equal(100, cappedData.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Details_ReturnsAuthorizedImageUrlAndNeverLeaksStoragePath()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var memory = await CreateMemoryAsync(owner, "Có ảnh", "Bình yên", DateTime.UtcNow.Date, "Hà Nội", ["riêng tư"]);
        var image = await CreateImageAsync(memory);

        using var ownerClient = await CreateBearerClientAsync(owner);
        using var response = await ownerClient.GetAsync($"/api/v1/memories/{memory.Id}");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var imageJson = document.RootElement.GetProperty("data").GetProperty("images")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"/api/v1/images/{image.Id}/content", imageJson.GetProperty("contentUrl").GetString());
        Assert.DoesNotContain(image.ImagePath, body, StringComparison.Ordinal);
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);

        using var otherClient = await CreateBearerClientAsync(other);
        using var otherResponse = await otherClient.GetAsync($"/api/v1/memories/{memory.Id}");
        using var adminClient = await CreateBearerClientAsync(admin);
        using var adminResponse = await adminClient.GetAsync($"/api/v1/memories/{memory.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesTagsPreservesImages_AndRejectsForgedOrDeletedMemory()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var memory = await CreateMemoryAsync(owner, "Bản cũ", "Buồn", new DateTime(2026, 6, 1), null, ["old"]);
        var image = await CreateImageAsync(memory);
        using var ownerClient = await CreateBearerClientAsync(owner);

        using var updateResponse = await ownerClient.PutAsJsonAsync($"/api/v1/memories/{memory.Id}", new
        {
            title = "Bản mới",
            story = "Đã cập nhật",
            feeling = "Vui vẻ",
            memoryDate = "2026-07-03",
            location = "Đà Nẵng",
            tags = new[] { "new", "NEW", "travel" }
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updated = await dbContext.Memories.Include(item => item.Images).Include(item => item.MemoryTags)
                .ThenInclude(item => item.Tag).SingleAsync(item => item.Id == memory.Id);
            Assert.Equal("Bản mới", updated.Title);
            Assert.Single(updated.Images);
            Assert.Equal(image.Id, updated.Images.Single().Id);
            Assert.Equal(["new", "travel"], updated.MemoryTags.Select(item => item.Tag.Name).OrderBy(name => name).ToArray());
        }

        using var otherClient = await CreateBearerClientAsync(other);
        using var forgedResponse = await otherClient.PutAsJsonAsync($"/api/v1/memories/{memory.Id}", ValidRequest());
        Assert.Equal(HttpStatusCode.NotFound, forgedResponse.StatusCode);

        await MarkDeletedAsync(memory.Id);
        using var deletedResponse = await ownerClient.PutAsJsonAsync($"/api/v1/memories/{memory.Id}", ValidRequest());
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAndRestore_AreOwnerScopedKeepImageFileAndRestorePrivateAccess()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var memory = await CreateMemoryAsync(owner, "Khôi phục", "Bình yên", DateTime.UtcNow.Date, null, []);
        var image = await CreateImageAsync(memory);
        var imagePath = ResolveImagePath(image.ImagePath);
        using var ownerClient = await CreateBearerClientAsync(owner);
        using var otherClient = await CreateBearerClientAsync(other);

        using var forgedDelete = await otherClient.DeleteAsync($"/api/v1/memories/{memory.Id}");
        Assert.Equal(HttpStatusCode.NotFound, forgedDelete.StatusCode);
        using var deleteResponse = await ownerClient.DeleteAsync($"/api/v1/memories/{memory.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.True(File.Exists(imagePath));

        using var hiddenList = await ownerClient.GetAsync("/api/v1/memories");
        using var hiddenDetails = await ownerClient.GetAsync($"/api/v1/memories/{memory.Id}");
        Assert.DoesNotContain("Khôi phục", await hiddenList.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, hiddenDetails.StatusCode);

        using var forgedRestore = await otherClient.PostAsync($"/api/v1/memories/{memory.Id}/restore", null);
        Assert.Equal(HttpStatusCode.NotFound, forgedRestore.StatusCode);
        using var restoreResponse = await ownerClient.PostAsync($"/api/v1/memories/{memory.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        using var restoredDetails = await ownerClient.GetAsync($"/api/v1/memories/{memory.Id}");
        Assert.Equal(HttpStatusCode.OK, restoredDetails.StatusCode);
        Assert.Contains($"/api/v1/images/{image.Id}/content", await restoredDetails.Content.ReadAsStringAsync());

        using var cookieClient = await CreateCookieClientAsync(owner);
        using var imageResponse = await cookieClient.GetAsync($"/Images/MemoryImage/{image.Id}");
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
    }

    private async Task<TestUser> CreateUserAsync(string displayName, bool addAdminRole = false)
    {
        var email = $"memory-api-{Guid.NewGuid():N}@example.test";
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
        if (addAdminRole)
        {
            Assert.True((await userManager.AddToRoleAsync(user, IdentitySeedData.AdminRole)).Succeeded);
        }

        return new TestUser(user.Id, email);
    }

    private async Task<Memory> CreateMemoryAsync(TestUser user, string title, string feeling, DateTime memoryDate, string? location, IReadOnlyList<string> tags)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var memory = new Memory
        {
            UserId = user.Id,
            Title = title,
            Story = $"Câu chuyện: {title}",
            Feeling = feeling,
            MemoryDate = memoryDate.Date,
            Location = location,
            CreatedAt = now,
            UpdatedAt = now
        };
        foreach (var tagName in tags)
        {
            var tag = await dbContext.Tags.FirstOrDefaultAsync(item => item.Name == tagName) ?? new Tag { Name = tagName };
            memory.MemoryTags.Add(new MemoryTag { Memory = memory, Tag = tag });
        }

        dbContext.Memories.Add(memory);
        await dbContext.SaveChangesAsync();
        return memory;
    }

    private async Task<MemoryImage> CreateImageAsync(Memory memory)
    {
        var imagePath = $"uploads/tests/{Guid.NewGuid():N}.png";
        var physicalPath = ResolveImagePath(imagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        await File.WriteAllBytesAsync(physicalPath, [137, 80, 78, 71, 13, 10, 26, 10]);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var image = new MemoryImage
        {
            MemoryId = memory.Id,
            ImagePath = imagePath,
            OriginalFileName = "test-image.png",
            UploadedAt = DateTime.UtcNow
        };
        dbContext.MemoryImages.Add(image);
        await dbContext.SaveChangesAsync();
        return image;
    }

    private async Task MarkDeletedAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = await dbContext.Memories.SingleAsync(item => item.Id == memoryId);
        memory.IsDeleted = true;
        memory.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task<HttpClient> CreateBearerClientAsync(TestUser user)
    {
        var client = CreateClient();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var applicationUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(applicationUser);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokenService.GenerateAccessTokenAsync(applicationUser));
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
        var token = System.Text.RegularExpressions.Regex.Match(loginPage,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));
        using var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("__RequestVerificationToken", System.Net.WebUtility.HtmlDecode(token)),
            new KeyValuePair<string, string>("Email", user.Email),
            new KeyValuePair<string, string>("Password", Password),
            new KeyValuePair<string, string>("RememberMe", "false")
        ]));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        return client;
    }

    private string ResolveImagePath(string imagePath)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        return storage.ResolveImagePath(imagePath) ?? throw new InvalidOperationException("Không thể tìm đường dẫn ảnh test.");
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static object ValidRequest() => new
    {
        title = "Kỷ niệm hợp lệ",
        story = "Nội dung hợp lệ",
        feeling = "Bình yên",
        memoryDate = "2026-07-01",
        location = "Hà Nội",
        tags = new[] { "test" }
    };

    private sealed record TestUser(string Id, string Email);
}
