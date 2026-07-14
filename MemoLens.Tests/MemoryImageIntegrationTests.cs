using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoLens.Tests;

public class MemoryImageIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Password = "MemoLens1";
    private static readonly byte[] TinyPng = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13];
    private readonly CustomWebApplicationFactory _factory;

    public MemoryImageIntegrationTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task ValidImageUpload_StoresPrivateMetadataAndServesOwnerBytes()
    {
        var user = await CreateUserAsync("Image owner");
        using var client = await CreateAuthenticatedClientAsync(user);

        var memoryId = await CreateMemoryWithImagesAsync(
            client,
            "Kỷ niệm có ảnh hợp lệ",
            [new TestImageFile("anh ky niem.png", TinyPng)]);

        var image = Assert.Single(await GetImagesAsync(memoryId));
        var physicalPath = ResolveImagePath(image.ImagePath);

        Assert.Equal("anh ky niem.png", image.OriginalFileName);
        Assert.True(File.Exists(physicalPath));
        Assert.Equal(TinyPng, await File.ReadAllBytesAsync(physicalPath));
        Assert.NotEqual(image.OriginalFileName, Path.GetFileName(image.ImagePath));
        Assert.DoesNotContain("..", image.ImagePath, StringComparison.Ordinal);
        Assert.StartsWith("uploads/memories/", image.ImagePath, StringComparison.Ordinal);

        using var imageResponse = await client.GetAsync($"/Images/MemoryImage/{image.Id}");
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyPng, await imageResponse.Content.ReadAsByteArrayAsync());

        using var detailsResponse = await client.GetAsync($"/Memories/Details/{memoryId}");
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        Assert.Contains($"/Images/MemoryImage/{image.Id}", detailsHtml);
        Assert.DoesNotContain(image.ImagePath, detailsHtml);
    }

    [Fact]
    public async Task MultipleValidImages_CreateAllRowsAndPrivateFilesInUploadOrder()
    {
        var user = await CreateUserAsync("Multiple image owner");
        using var client = await CreateAuthenticatedClientAsync(user);

        var files = new[]
        {
            new TestImageFile("one.jpg", [1, 2, 3]),
            new TestImageFile("two.png", TinyPng),
            new TestImageFile("three.webp", [4, 5, 6])
        };

        var memoryId = await CreateMemoryWithImagesAsync(client, "Nhiều ảnh hợp lệ", files);
        var images = (await GetImagesAsync(memoryId)).OrderBy(image => image.UploadedAt).ToList();

        Assert.Equal(files.Length, images.Count);
        Assert.Equal(files.Select(file => file.FileName), images.Select(image => image.OriginalFileName));
        Assert.All(images, image => Assert.True(File.Exists(ResolveImagePath(image.ImagePath))));
        Assert.Equal(files.Length, GetStoredFilePaths().Count);
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    public async Task AllowedImageExtensions_AreAccepted(string fileName)
    {
        var user = await CreateUserAsync($"Extension {fileName}");
        using var client = await CreateAuthenticatedClientAsync(user);

        var memoryId = await CreateMemoryWithImagesAsync(
            client,
            $"Ảnh {fileName}",
            [new TestImageFile(fileName, TinyPng)]);

        var image = Assert.Single(await GetImagesAsync(memoryId));
        Assert.Equal(fileName, image.OriginalFileName);
        Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));
    }

    [Fact]
    public async Task InvalidExtension_IsRejectedWithoutDatabaseOrPhysicalFileChanges()
    {
        var user = await CreateUserAsync("Invalid extension owner");
        using var client = await CreateAuthenticatedClientAsync(user);
        var title = $"Không nhận file {Guid.NewGuid():N}";

        using var response = await SubmitCreateMemoryAsync(
            client,
            title,
            [new TestImageFile("not-an-image.txt", Encoding.UTF8.GetBytes("not an image"))]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("JPG, PNG, WEBP", await response.Content.ReadAsStringAsync());
        Assert.False(await MemoryWithTitleExistsAsync(title));
        Assert.Empty(GetStoredFilePaths());
    }

    [Fact]
    public async Task OversizedImage_IsRejectedWithoutDatabaseOrPhysicalFileChanges()
    {
        var user = await CreateUserAsync("Oversized owner");
        using var client = await CreateAuthenticatedClientAsync(user);
        var title = $"Ảnh vượt quá giới hạn {Guid.NewGuid():N}";

        using var response = await SubmitCreateMemoryAsync(
            client,
            title,
            [new TestImageFile("too-large.png", new byte[(5 * 1024 * 1024) + 1])]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("5MB", await response.Content.ReadAsStringAsync());
        Assert.False(await MemoryWithTitleExistsAsync(title));
        Assert.Empty(GetStoredFilePaths());
    }

    [Fact]
    public async Task ImageLimit_RejectsEleventhImageAndKeepsExistingImages()
    {
        var user = await CreateUserAsync("Image limit owner");
        using var client = await CreateAuthenticatedClientAsync(user);

        var originalFiles = Enumerable.Range(1, 10)
            .Select(index => new TestImageFile($"original-{index}.png", TinyPng))
            .ToArray();
        var memoryId = await CreateMemoryWithImagesAsync(client, "Mười ảnh", originalFiles);
        var originalImages = await GetImagesAsync(memoryId);
        var originalPaths = originalImages.Select(image => image.ImagePath).OrderBy(path => path).ToArray();

        using var response = await SubmitEditMemoryAsync(
            client,
            memoryId,
            [new TestImageFile("eleventh.png", TinyPng)]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("10", await response.Content.ReadAsStringAsync());
        var imagesAfterRejectedUpload = await GetImagesAsync(memoryId);
        Assert.Equal(10, imagesAfterRejectedUpload.Count);
        Assert.Equal(originalPaths, imagesAfterRejectedUpload.Select(image => image.ImagePath).OrderBy(path => path));
        Assert.Equal(10, GetStoredFilePaths().Count);
    }

    [Fact]
    public async Task ImageEndpoint_OnlyAllowsOwnerAndDoesNotLetAdminBypassOwnership()
    {
        var owner = await CreateUserAsync("Owner");
        var otherUser = await CreateUserAsync("Other user");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        using var ownerClient = await CreateAuthenticatedClientAsync(owner);
        using var otherClient = await CreateAuthenticatedClientAsync(otherUser);
        using var adminClient = await CreateAuthenticatedClientAsync(admin);

        var memoryId = await CreateMemoryWithImagesAsync(
            ownerClient,
            "Ảnh riêng tư của owner",
            [new TestImageFile("owner.png", TinyPng)]);
        var image = Assert.Single(await GetImagesAsync(memoryId));

        using var ownerResponse = await ownerClient.GetAsync($"/Images/MemoryImage/{image.Id}");
        using var otherResponse = await otherClient.GetAsync($"/Images/MemoryImage/{image.Id}");
        using var adminResponse = await adminClient.GetAsync($"/Images/MemoryImage/{image.Id}");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    [Fact]
    public async Task ImageEndpoint_GuestIsRedirectedToLogin()
    {
        var owner = await CreateUserAsync("Guest image owner");
        using var ownerClient = await CreateAuthenticatedClientAsync(owner);
        var memoryId = await CreateMemoryWithImagesAsync(
            ownerClient,
            "Ảnh cho guest test",
            [new TestImageFile("private.png", TinyPng)]);
        var image = Assert.Single(await GetImagesAsync(memoryId));
        using var guestClient = CreateClient();

        using var response = await guestClient.GetAsync($"/Images/MemoryImage/{image.Id}");

        AssertLoginRedirect(response);
    }

    [Fact]
    public async Task MissingPrivateFile_ReturnsNotFoundWithoutLeakingPath()
    {
        var user = await CreateUserAsync("Missing file owner");
        var memory = await CreateMemoryAsync(user, "Metadata without a file");
        var image = await CreateMetadataOnlyImageAsync(memory, "uploads/memories/missing/does-not-exist.png");
        using var client = await CreateAuthenticatedClientAsync(user);

        using var response = await client.GetAsync($"/Images/MemoryImage/{image.Id}");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(_factory.TestUploadRootPath, responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(image.ImagePath, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SoftDeletedMemory_HidesImageUntilRestoreWithoutDeletingPhysicalFile()
    {
        var user = await CreateUserAsync("Restore image owner");
        using var client = await CreateAuthenticatedClientAsync(user);
        var memoryId = await CreateMemoryWithImagesAsync(
            client,
            "Ảnh trước khi xóa mềm",
            [new TestImageFile("restore.png", TinyPng)]);
        var image = Assert.Single(await GetImagesAsync(memoryId));
        var physicalPath = ResolveImagePath(image.ImagePath);

        using (var beforeDeleteResponse = await client.GetAsync($"/Images/MemoryImage/{image.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, beforeDeleteResponse.StatusCode);
        }

        var deleteToken = await GetAntiForgeryTokenAsync(client, $"/Memories/Delete/{memoryId}");
        using (var deleteResponse = await client.PostAsync(
                   $"/Memories/Delete/{memoryId}",
                   Form(("__RequestVerificationToken", deleteToken))))
        {
            Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        }

        using (var hiddenResponse = await client.GetAsync($"/Images/MemoryImage/{image.Id}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
        }
        Assert.True(File.Exists(physicalPath));

        var restoreToken = await GetAntiForgeryTokenAsync(client, "/Trash");
        using (var restoreResponse = await client.PostAsync(
                   $"/Trash/RestoreMemory/{memoryId}",
                   Form(("__RequestVerificationToken", restoreToken))))
        {
            Assert.Equal(HttpStatusCode.Redirect, restoreResponse.StatusCode);
        }

        using var restoredResponse = await client.GetAsync($"/Images/MemoryImage/{image.Id}");
        Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);
        Assert.True(File.Exists(physicalPath));
    }

    [Fact]
    public async Task DeleteImage_RemovesOnlyRequestedImageAndHandlesMissingFile()
    {
        var user = await CreateUserAsync("Delete image owner");
        using var ownerClient = await CreateAuthenticatedClientAsync(user);
        var memoryId = await CreateMemoryWithImagesAsync(
            ownerClient,
            "Xóa từng ảnh",
            [
                new TestImageFile("first.png", TinyPng),
                new TestImageFile("second.png", [9, 8, 7])
            ]);
        var images = (await GetImagesAsync(memoryId)).OrderBy(image => image.OriginalFileName).ToList();
        var firstImage = images[0];
        var secondImage = images[1];
        var firstPath = ResolveImagePath(firstImage.ImagePath);
        var secondPath = ResolveImagePath(secondImage.ImagePath);
        File.Delete(firstPath);

        var token = await GetAntiForgeryTokenAsync(ownerClient, $"/Memories/Edit/{memoryId}");
        using (var deleteResponse = await ownerClient.PostAsync(
                   $"/Memories/DeleteImage/{memoryId}?imageId={firstImage.Id}",
                   Form(("__RequestVerificationToken", token))))
        {
            Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        }

        var imagesAfterDelete = await GetImagesAsync(memoryId);
        Assert.Single(imagesAfterDelete);
        Assert.Equal(secondImage.Id, imagesAfterDelete[0].Id);
        Assert.False(File.Exists(firstPath));
        Assert.True(File.Exists(secondPath));

        var otherUser = await CreateUserAsync("Forged delete user");
        using var otherClient = await CreateAuthenticatedClientAsync(otherUser);
        var forgedToken = await GetAntiForgeryTokenAsync(otherClient, "/Settings/EditProfile");
        using var forgedResponse = await otherClient.PostAsync(
            $"/Memories/DeleteImage/{memoryId}?imageId={secondImage.Id}",
            Form(("__RequestVerificationToken", forgedToken)));

        Assert.Equal(HttpStatusCode.NotFound, forgedResponse.StatusCode);
        Assert.Single(await GetImagesAsync(memoryId));
        Assert.True(File.Exists(secondPath));
    }

    [Fact]
    public async Task UnsafeFileNames_AreStoredUnderGeneratedPrivatePaths()
    {
        var user = await CreateUserAsync("Unsafe filename owner");
        using var client = await CreateAuthenticatedClientAsync(user);
        const string readableUnsafeName = "ảnh kỷ niệm..mùa hè.png";

        var memoryId = await CreateMemoryWithImagesAsync(
            client,
            "Tên file an toàn",
            [new TestImageFile(readableUnsafeName, TinyPng)]);
        var uploadedImage = Assert.Single(await GetImagesAsync(memoryId));

        Assert.Equal(readableUnsafeName, uploadedImage.OriginalFileName);
        Assert.Matches("^uploads/memories/[^/]+/[0-9]+/[a-f0-9]{32}\\.png$", uploadedImage.ImagePath);
        AssertPathIsInsideTestUploadRoot(ResolveImagePath(uploadedImage.ImagePath));

        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        await using var stream = new MemoryStream(TinyPng);
        var pathLikeFile = new FormFile(stream, 0, TinyPng.Length, "NewImages", "../outside.png");
        var stored = await storage.SaveImageAsync(pathLikeFile, user.Id, memoryId);

        Assert.Equal("outside.png", stored.OriginalFileName);
        Assert.DoesNotContain("..", stored.ImagePath, StringComparison.Ordinal);
        AssertPathIsInsideTestUploadRoot(ResolveImagePath(stored.ImagePath));
    }

    [Fact]
    public async Task PrivateImages_AreNotAvailableThroughStaticUploadUrls()
    {
        var user = await CreateUserAsync("Static path owner");
        using var client = await CreateAuthenticatedClientAsync(user);
        var memoryId = await CreateMemoryWithImagesAsync(
            client,
            "Không có static url",
            [new TestImageFile("no-static.png", TinyPng)]);
        var image = Assert.Single(await GetImagesAsync(memoryId));

        using var staticResponse = await client.GetAsync($"/{image.ImagePath}");
        using var endpointResponse = await client.GetAsync($"/Images/MemoryImage/{image.Id}");

        Assert.Equal(HttpStatusCode.NotFound, staticResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, endpointResponse.StatusCode);
    }

    private async Task<TestUser> CreateUserAsync(string displayName, bool addAdminRole = false)
    {
        var email = $"image-tests-{Guid.NewGuid():N}@example.test";
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

        var createResult = await userManager.CreateAsync(user, Password);
        Assert.True(createResult.Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, IdentitySeedData.UserRole)).Succeeded);

        if (addAdminRole)
        {
            Assert.True((await userManager.AddToRoleAsync(user, IdentitySeedData.AdminRole)).Succeeded);
        }

        return new TestUser(user.Id, email);
    }

    private async Task<int> CreateMemoryWithImagesAsync(
        HttpClient client,
        string title,
        IReadOnlyCollection<TestImageFile> files)
    {
        using var response = await SubmitCreateMemoryAsync(client, title, files);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location!;
        var detailsUri = location.IsAbsoluteUri
            ? location
            : new Uri(client.BaseAddress!, location);
        return int.Parse(detailsUri.Segments[^1]);
    }

    private async Task<HttpResponseMessage> SubmitCreateMemoryAsync(
        HttpClient client,
        string title,
        IReadOnlyCollection<TestImageFile> files)
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/Memories/Create");
        return await client.PostAsync("/Memories/Create", CreateMemoryForm(antiForgeryToken, title, files));
    }

    private async Task<HttpResponseMessage> SubmitEditMemoryAsync(
        HttpClient client,
        int memoryId,
        IReadOnlyCollection<TestImageFile> files)
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, $"/Memories/Edit/{memoryId}");
        var memory = await GetMemoryAsync(memoryId);
        using var form = CreateMemoryForm(antiForgeryToken, memory.Title, files);
        form.Add(new StringContent(memoryId.ToString()), "Id");
        return await client.PostAsync($"/Memories/Edit/{memoryId}", form);
    }

    private static MultipartFormDataContent CreateMemoryForm(
        string antiForgeryToken,
        string title,
        IReadOnlyCollection<TestImageFile> files)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
        form.Add(new StringContent(title), "Title");
        form.Add(new StringContent("Bình yên"), "Feeling");
        form.Add(new StringContent("2026-07-14"), "MemoryDate");
        form.Add(new StringContent("Đà Lạt"), "Location");
        form.Add(new StringContent("image-test"), "TagsText");

        foreach (var file in files)
        {
            var content = new ByteArrayContent(file.Bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(content, "NewImages", file.FileName);
        }

        return form;
    }

    private async Task<Memory> CreateMemoryAsync(TestUser user, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var memory = new Memory
        {
            UserId = user.Id,
            Title = title,
            Feeling = "Bình yên",
            MemoryDate = now.Date,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Memories.Add(memory);
        await dbContext.SaveChangesAsync();
        return memory;
    }

    private async Task<MemoryImage> CreateMetadataOnlyImageAsync(Memory memory, string imagePath)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var image = new MemoryImage
        {
            MemoryId = memory.Id,
            ImagePath = imagePath,
            OriginalFileName = "missing.png",
            UploadedAt = DateTime.UtcNow
        };

        dbContext.MemoryImages.Add(image);
        await dbContext.SaveChangesAsync();
        return image;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(TestUser user)
    {
        var client = CreateClient();
        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        using var loginResponse = await client.PostAsync(
            "/Account/Login",
            Form(
                ("__RequestVerificationToken", antiForgeryToken),
                ("Email", user.Email),
                ("Password", Password),
                ("RememberMe", "false")));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        return client;
    }

    private async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractAntiForgeryToken(await response.Content.ReadAsStringAsync());
    }

    private async Task<IReadOnlyList<MemoryImage>> GetImagesAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.MemoryImages
            .AsNoTracking()
            .Where(image => image.MemoryId == memoryId)
            .ToListAsync();
    }

    private async Task<Memory> GetMemoryAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Memories.AsNoTracking().SingleAsync(memory => memory.Id == memoryId);
    }

    private async Task<bool> MemoryWithTitleExistsAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Memories.AnyAsync(memory => memory.Title == title);
    }

    private IReadOnlyList<string> GetStoredFilePaths()
    {
        return !Directory.Exists(_factory.TestUploadRootPath)
            ? []
            : Directory.GetFiles(_factory.TestUploadRootPath, "*", SearchOption.AllDirectories);
    }

    private string ResolveImagePath(string imagePath)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
        return storage.ResolveImagePath(imagePath)
            ?? throw new InvalidOperationException("The test image path could not be resolved.");
    }

    private void AssertPathIsInsideTestUploadRoot(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_factory.TestUploadRootPath, fullPath);
        Assert.False(Path.IsPathRooted(relativePath));
        Assert.False(relativePath.StartsWith("..", StringComparison.Ordinal));
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] values)
    {
        return new FormUrlEncodedContent(values.Select(value =>
            new KeyValuePair<string, string>(value.Name, value.Value)));
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "The page did not contain an anti-forgery token.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static void AssertLoginRedirect(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.OriginalString);
    }

    private sealed record TestUser(string Id, string Email);

    private sealed record TestImageFile(string FileName, byte[] Bytes);
}
