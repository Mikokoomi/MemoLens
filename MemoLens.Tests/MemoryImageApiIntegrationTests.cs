using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Services;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoLens.Tests;

public sealed class MemoryImageApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Password = "MemoLens1";
    private static readonly byte[] TinyPng = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly CustomWebApplicationFactory _factory;

    public MemoryImageApiIntegrationTests(CustomWebApplicationFactory factory)
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
    [InlineData("POST", "/api/v1/memories/1/images")]
    [InlineData("GET", "/api/v1/images/1/content")]
    [InlineData("DELETE", "/api/v1/memories/1/images/1")]
    public async Task ImageEndpoints_WithoutBearerToken_ReturnUnauthorized(string method, string path)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = new MultipartFormDataContent();
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("success", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_ValidBatchStoresSafePrivateFilesAndReturnsJwtContentUrlsInOrder()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Nhiều ảnh");
        using var client = await CreateBearerClientAsync(owner);
        var files = new[]
        {
            new TestFile("mot.jpg", [1, 2, 3]),
            new TestFile("hai.jpeg", [4, 5, 6]),
            new TestFile("ba.png", TinyPng),
            new TestFile("bon.webp", [7, 8, 9])
        };

        using var response = await UploadAsync(client, memory.Id, files);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");
        var responseImages = data.GetProperty("images").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(4, data.GetProperty("totalImageCount").GetInt32());
        Assert.Equal(6, data.GetProperty("remainingSlots").GetInt32());
        Assert.Equal(files.Select(file => file.FileName), responseImages.Select(item => item.GetProperty("originalFileName").GetString()));

        var images = await GetImagesAsync(memory.Id);
        Assert.Equal(4, images.Count);
        Assert.Equal(files.Select(file => file.FileName), images.Select(image => image.OriginalFileName));
        Assert.Equal(images.Select(image => $"/api/v1/images/{image.Id}/content"), responseImages.Select(item => item.GetProperty("contentUrl").GetString()));
        Assert.All(images, image =>
        {
            Assert.StartsWith($"uploads/memories/{owner.Id}/{memory.Id}/", image.ImagePath, StringComparison.Ordinal);
            Assert.NotEqual(image.OriginalFileName, Path.GetFileName(image.ImagePath));
            Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));
            Assert.DoesNotContain(image.ImagePath, body, StringComparison.Ordinal);
        });
        Assert.DoesNotContain("imagePath", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("single.jpg")]
    [InlineData("single.jpeg")]
    [InlineData("single.png")]
    [InlineData("single.webp")]
    public async Task Upload_SingleAllowedExtensionIsAccepted(string fileName)
    {
        var owner = await CreateUserAsync("Single image owner");
        var memory = await CreateMemoryAsync(owner, "Một ảnh");
        using var client = await CreateBearerClientAsync(owner);

        using var response = await UploadAsync(client, memory.Id, [new TestFile(fileName, TinyPng)]);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var image = Assert.Single(await GetImagesAsync(memory.Id));
        Assert.Equal(fileName, image.OriginalFileName);
        Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));
    }

    [Fact]
    public async Task Upload_OtherUserAndAdminCannotUseAnotherUsersMemory()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var memory = await CreateMemoryAsync(owner, "Riêng tư");

        using var otherClient = await CreateBearerClientAsync(other);
        using var otherResponse = await UploadAsync(otherClient, memory.Id, [new TestFile("other.png", TinyPng)]);
        using var adminClient = await CreateBearerClientAsync(admin);
        using var adminResponse = await UploadAsync(adminClient, memory.Id, [new TestFile("admin.png", TinyPng)]);

        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
        Assert.Empty(await GetImagesAsync(memory.Id));
        Assert.Empty(GetStoredFiles());
    }

    [Fact]
    public async Task Upload_NoFileUnsupportedOversizeAndMixedBatchAreAtomic()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Kiểm tra validation");
        using var client = await CreateBearerClientAsync(owner);

        using var noFileResponse = await UploadAsync(client, memory.Id, []);
        Assert.Equal(HttpStatusCode.BadRequest, noFileResponse.StatusCode);

        using var unsupportedResponse = await UploadAsync(client, memory.Id, [new TestFile("script.exe", [1])]);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedResponse.StatusCode);

        using var oversizedResponse = await UploadAsync(client, memory.Id, [new TestFile("large.jpg", new byte[(5 * 1024 * 1024) + 1])]);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedResponse.StatusCode);

        using var mixedResponse = await UploadAsync(client, memory.Id,
        [
            new TestFile("valid.png", TinyPng),
            new TestFile("invalid.txt", [9, 9, 9])
        ]);
        Assert.Equal(HttpStatusCode.BadRequest, mixedResponse.StatusCode);

        Assert.Empty(await GetImagesAsync(memory.Id));
        Assert.Empty(GetStoredFiles());
    }

    [Fact]
    public async Task Upload_ExceedingTenImagesRejectsWholeBatchAndKeepsExistingImages()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Giới hạn ảnh");
        for (var index = 0; index < 9; index++)
        {
            await CreateStoredImageAsync(memory, $"existing-{index}.png", [Convert.ToByte(index)]);
        }

        var existingPaths = (await GetImagesAsync(memory.Id)).Select(image => image.ImagePath).ToArray();
        using var client = await CreateBearerClientAsync(owner);
        using var response = await UploadAsync(client, memory.Id,
        [
            new TestFile("new-one.png", TinyPng),
            new TestFile("new-two.png", TinyPng)
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(existingPaths, (await GetImagesAsync(memory.Id)).Select(image => image.ImagePath));
        Assert.Equal(9, GetStoredFiles().Count);
    }

    [Fact]
    public async Task Content_ReturnsExactBytesTypeAndPrivateNoStoreWithoutLeakingPaths()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var memory = await CreateMemoryAsync(owner, "Ảnh riêng tư");
        var image = await CreateStoredImageAsync(memory, "photo.png", TinyPng);

        using var ownerClient = await CreateBearerClientAsync(owner);
        using var ownerResponse = await ownerClient.GetAsync($"/api/v1/images/{image.Id}/content");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal("image/png", ownerResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyPng, await ownerResponse.Content.ReadAsByteArrayAsync());
        Assert.Contains("private", ownerResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", ownerResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(ownerResponse.Content.Headers.ContentDisposition);
        Assert.DoesNotContain(image.ImagePath, ownerResponse.Headers.ToString(), StringComparison.Ordinal);

        using var otherClient = await CreateBearerClientAsync(other);
        using var otherResponse = await otherClient.GetAsync($"/api/v1/images/{image.Id}/content");
        using var adminClient = await CreateBearerClientAsync(admin);
        using var adminResponse = await adminClient.GetAsync($"/api/v1/images/{image.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Content_MissingFileAndDeletedMemoryReturnNotFoundThenRestoreReturnsImage()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Khôi phục ảnh");
        var image = await CreateStoredImageAsync(memory, "restore.webp", [4, 3, 2, 1]);
        using var client = await CreateBearerClientAsync(owner);

        await SetMemoryDeletedAsync(memory.Id, isDeleted: true);
        using var deletedResponse = await client.GetAsync($"/api/v1/images/{image.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);

        await SetMemoryDeletedAsync(memory.Id, isDeleted: false);
        using var restoredResponse = await client.GetAsync($"/api/v1/images/{image.Id}/content");
        Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);

        File.Delete(ResolveImagePath(image.ImagePath));
        using var missingFileResponse = await client.GetAsync($"/api/v1/images/{image.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, missingFileResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesOnlyRequestedRowAndFileThenRepeatedDeleteReturnsNotFound()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Xóa một ảnh");
        var first = await CreateStoredImageAsync(memory, "first.jpg", [1]);
        var second = await CreateStoredImageAsync(memory, "second.jpg", [2]);
        var firstPath = ResolveImagePath(first.ImagePath);
        var secondPath = ResolveImagePath(second.ImagePath);
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{first.Id}");
        using var repeatedResponse = await client.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{first.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeatedResponse.StatusCode);
        Assert.False(File.Exists(firstPath));
        Assert.True(File.Exists(secondPath));
        var remaining = Assert.Single(await GetImagesAsync(memory.Id));
        Assert.Equal(second.Id, remaining.Id);
    }

    [Fact]
    public async Task Delete_OtherUserAndAdminReturnNotFoundAndMissingPhysicalFileStillDeletesRow()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var memory = await CreateMemoryAsync(owner, "Xóa riêng tư");
        var image = await CreateStoredImageAsync(memory, "private.jpeg", [8, 8]);

        using var otherClient = await CreateBearerClientAsync(other);
        using var otherResponse = await otherClient.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{image.Id}");
        using var adminClient = await CreateBearerClientAsync(admin);
        using var adminResponse = await adminClient.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);

        File.Delete(ResolveImagePath(image.ImagePath));
        using var ownerClient = await CreateBearerClientAsync(owner);
        using var ownerResponse = await ownerClient.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Empty(await GetImagesAsync(memory.Id));
    }

    [Fact]
    public async Task UploadAndDelete_SoftDeletedMemoryReturnNotFound()
    {
        var owner = await CreateUserAsync("Owner");
        var memory = await CreateMemoryAsync(owner, "Đã xóa");
        var image = await CreateStoredImageAsync(memory, "deleted.png", TinyPng);
        await SetMemoryDeletedAsync(memory.Id, isDeleted: true);
        using var client = await CreateBearerClientAsync(owner);

        using var uploadResponse = await UploadAsync(client, memory.Id, [new TestFile("new.png", TinyPng)]);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/memories/{memory.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NotFound, uploadResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        Assert.Single(await GetImagesAsync(memory.Id));
        Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));
    }

    private async Task<TestUser> CreateUserAsync(string displayName, bool addAdminRole = false)
    {
        var email = $"memory-image-api-{Guid.NewGuid():N}@example.test";
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

    private async Task<Memory> CreateMemoryAsync(TestUser owner, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = new Memory
        {
            UserId = owner.Id,
            Title = title,
            Feeling = "Bình yên",
            MemoryDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Memories.Add(memory);
        await context.SaveChangesAsync();
        return memory;
    }

    private async Task<MemoryImage> CreateStoredImageAsync(Memory memory, string originalFileName, byte[] bytes)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        var extension = Path.GetExtension(originalFileName);
        var relativePath = $"uploads/memories/{memory.UserId}/{memory.Id}/{Guid.NewGuid():N}{extension}";
        var physicalPath = storage.ResolveImagePath(relativePath)!;
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        await File.WriteAllBytesAsync(physicalPath, bytes);

        var image = new MemoryImage
        {
            MemoryId = memory.Id,
            ImagePath = relativePath,
            OriginalFileName = originalFileName,
            UploadedAt = DateTime.UtcNow.AddTicks(await context.MemoryImages.CountAsync())
        };
        context.MemoryImages.Add(image);
        await context.SaveChangesAsync();
        return image;
    }

    private async Task<List<MemoryImage>> GetImagesAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.MemoryImages
            .AsNoTracking()
            .Where(image => image.MemoryId == memoryId)
            .OrderBy(image => image.UploadedAt)
            .ToListAsync();
    }

    private async Task SetMemoryDeletedAsync(int memoryId, bool isDeleted)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = await context.Memories.SingleAsync(item => item.Id == memoryId);
        memory.IsDeleted = isDeleted;
        memory.DeletedAt = isDeleted ? DateTime.UtcNow : null;
        await context.SaveChangesAsync();
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

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        int memoryId,
        IReadOnlyCollection<TestFile> files)
    {
        var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent(file.Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMediaType(file.FileName));
            content.Add(fileContent, "files", file.FileName);
        }

        return await client.PostAsync($"/api/v1/memories/{memoryId}/images", content);
    }

    private string ResolveImagePath(string imagePath)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IImageStorageService>().ResolveImagePath(imagePath)!;
    }

    private List<string> GetStoredFiles()
    {
        return Directory.Exists(_factory.TestUploadRootPath)
            ? Directory.GetFiles(_factory.TestUploadRootPath, "*", SearchOption.AllDirectories).ToList()
            : [];
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static string GetMediaType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private sealed record TestUser(string Id, string Email);

    private sealed record TestFile(string FileName, byte[] Bytes);
}
