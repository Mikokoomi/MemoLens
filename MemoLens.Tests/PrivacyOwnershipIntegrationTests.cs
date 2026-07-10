using System.Net;
using System.Text.RegularExpressions;
using MemoLens.Data;
using MemoLens.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoLens.Tests;

public class PrivacyOwnershipIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "MemoLens1";
    private readonly CustomWebApplicationFactory _factory;

    public PrivacyOwnershipIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Memories_AreVisibleOnlyToTheirOwner()
    {
        var userA = await CreateUserAsync("User A");
        var userB = await CreateUserAsync("User B");
        var memory = await CreateMemoryAsync(userA, "PrivateMemoryA");

        using var ownerClient = await CreateAuthenticatedClientAsync(userA);
        using var otherClient = await CreateAuthenticatedClientAsync(userB);
        using var guestClient = CreateClient();

        using var ownerResponse = await ownerClient.GetAsync($"/Memories/Details/{memory.Id}");
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Contains(memory.Title, ownerBody);
        Assert.Contains(memory.Story!, ownerBody);
        Assert.Contains(memory.Location!, ownerBody);

        foreach (var route in new[]
        {
            $"/Memories/Details/{memory.Id}",
            $"/Memories/Edit/{memory.Id}",
            $"/Memories/Delete/{memory.Id}"
        })
        {
            using var response = await otherClient.GetAsync(route);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(memory.Title, body);
            Assert.DoesNotContain(memory.Story!, body);
        }

        var antiForgeryToken = await GetAntiForgeryTokenAsync(otherClient);
        using var forgedDeleteResponse = await otherClient.PostAsync(
            $"/Memories/Delete/{memory.Id}",
            Form(("__RequestVerificationToken", antiForgeryToken)));

        Assert.Equal(HttpStatusCode.NotFound, forgedDeleteResponse.StatusCode);
        Assert.False(await IsMemoryDeletedAsync(memory.Id));

        using var guestResponse = await guestClient.GetAsync($"/Memories/Details/{memory.Id}");
        AssertLoginRedirect(guestResponse);
    }

    [Fact]
    public async Task Albums_AreOwnerScoped_AndForgedCrossOwnerAddDoesNotCreateRelationship()
    {
        var userA = await CreateUserAsync("User A");
        var userB = await CreateUserAsync("User B");
        var memoryA = await CreateMemoryAsync(userA, "Ký ức chỉ của User A");
        var albumA = await CreateAlbumAsync(userA, "Album riêng User A");
        var albumB = await CreateAlbumAsync(userB, "Album riêng User B");

        using var ownerClient = await CreateAuthenticatedClientAsync(userA);
        using var otherClient = await CreateAuthenticatedClientAsync(userB);

        using var ownerResponse = await ownerClient.GetAsync($"/Albums/Details/{albumA.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);

        foreach (var route in new[]
        {
            $"/Albums/Details/{albumA.Id}",
            $"/Albums/Edit/{albumA.Id}",
            $"/Albums/Delete/{albumA.Id}"
        })
        {
            using var response = await otherClient.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        var antiForgeryToken = await GetAntiForgeryTokenAsync(otherClient);
        using var forgedAddResponse = await otherClient.PostAsync(
            $"/Albums/AddMemories/{albumB.Id}",
            Form(
                ("__RequestVerificationToken", antiForgeryToken),
                ("AlbumId", albumB.Id.ToString()),
                ("SelectedMemoryIds", memoryA.Id.ToString())));

        Assert.Equal(HttpStatusCode.OK, forgedAddResponse.StatusCode);
        Assert.False(await AlbumContainsMemoryAsync(albumB.Id, memoryA.Id));
    }

    [Fact]
    public async Task Images_RequireOwner_AndSoftDeletedMemoryImagesStayHidden()
    {
        var userA = await CreateUserAsync("User A");
        var userB = await CreateUserAsync("User B");
        var memory = await CreateMemoryAsync(userA, "Ký ức có ảnh riêng");
        var image = await CreateImageAsync(memory);

        try
        {
            using var ownerClient = await CreateAuthenticatedClientAsync(userA);
            using var otherClient = await CreateAuthenticatedClientAsync(userB);
            using var guestClient = CreateClient();

            using var ownerResponse = await ownerClient.GetAsync($"/Images/MemoryImage/{image.Id}");
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            Assert.Equal("image/png", ownerResponse.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(await ownerResponse.Content.ReadAsByteArrayAsync());

            using var otherResponse = await otherClient.GetAsync($"/Images/MemoryImage/{image.Id}");
            Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
            Assert.NotEqual("image/png", otherResponse.Content.Headers.ContentType?.MediaType);

            using var guestResponse = await guestClient.GetAsync($"/Images/MemoryImage/{image.Id}");
            AssertLoginRedirect(guestResponse);

            await SoftDeleteMemoryAsync(memory.Id);
            using var deletedMemoryResponse = await ownerClient.GetAsync($"/Images/MemoryImage/{image.Id}");
            Assert.Equal(HttpStatusCode.NotFound, deletedMemoryResponse.StatusCode);
        }
        finally
        {
            var filePath = ResolveImagePath(image.ImagePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task Trash_ShowsAndRestoresOnlyTheCurrentUsersItems()
    {
        var userA = await CreateUserAsync("User A");
        var userB = await CreateUserAsync("User B");
        var deletedMemory = await CreateMemoryAsync(userA, "TrashMemoryA", isDeleted: true);
        var deletedAlbum = await CreateAlbumAsync(userA, "TrashAlbumA", isDeleted: true);

        using var ownerClient = await CreateAuthenticatedClientAsync(userA);
        using var otherClient = await CreateAuthenticatedClientAsync(userB);
        using var guestClient = CreateClient();

        using var ownerResponse = await ownerClient.GetAsync("/Trash");
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Contains(deletedMemory.Title, ownerBody);
        Assert.Contains(deletedAlbum.Title, ownerBody);

        using var otherResponse = await otherClient.GetAsync("/Trash");
        var otherBody = await otherResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);
        Assert.DoesNotContain(deletedMemory.Title, otherBody);
        Assert.DoesNotContain(deletedAlbum.Title, otherBody);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(otherClient);
        using var restoreMemoryResponse = await otherClient.PostAsync(
            $"/Trash/RestoreMemory/{deletedMemory.Id}",
            Form(("__RequestVerificationToken", antiForgeryToken)));
        using var restoreAlbumResponse = await otherClient.PostAsync(
            $"/Trash/RestoreAlbum/{deletedAlbum.Id}",
            Form(("__RequestVerificationToken", antiForgeryToken)));

        Assert.Equal(HttpStatusCode.NotFound, restoreMemoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, restoreAlbumResponse.StatusCode);
        Assert.True(await IsMemoryDeletedAsync(deletedMemory.Id));
        Assert.True(await IsAlbumDeletedAsync(deletedAlbum.Id));

        using var guestResponse = await guestClient.GetAsync("/Trash");
        AssertLoginRedirect(guestResponse);
    }

    [Fact]
    public async Task Settings_RequireLogin_AndExposeOnlyTheCurrentUsersAccount()
    {
        var userA = await CreateUserAsync("User A");
        var userB = await CreateUserAsync("User B");

        using var ownerClient = await CreateAuthenticatedClientAsync(userA);
        using var guestClient = CreateClient();

        using var ownerResponse = await ownerClient.GetAsync("/Settings");
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Contains(userA.Email, ownerBody);
        Assert.DoesNotContain(userB.Email, ownerBody);

        using var guestResponse = await guestClient.GetAsync("/Settings");
        AssertLoginRedirect(guestResponse);
    }

    [Fact]
    public async Task AdminRole_DoesNotBypassPrivateMemoryOwnership()
    {
        var userA = await CreateUserAsync("User A");
        var admin = await CreateUserAsync("Test Admin", addAdminRole: true);
        var memory = await CreateMemoryAsync(userA, "Ký ức không dành cho Admin");

        using var adminClient = await CreateAuthenticatedClientAsync(admin);
        using var response = await adminClient.GetAsync($"/Memories/Details/{memory.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(memory.Title, body);
    }

    private async Task<TestUser> CreateUserAsync(string displayName, bool addAdminRole = false)
    {
        var email = $"privacy-{Guid.NewGuid():N}@example.test";

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

    private async Task<Memory> CreateMemoryAsync(TestUser user, string title, bool isDeleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var memory = new Memory
        {
            UserId = user.Id,
            Title = title,
            Story = $"PrivateStory: {title}",
            Feeling = "Bình yên",
            MemoryDate = now.Date,
            Location = "PrivateLocation",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? now : null
        };

        dbContext.Memories.Add(memory);
        await dbContext.SaveChangesAsync();
        return memory;
    }

    private async Task<Album> CreateAlbumAsync(TestUser user, string title, bool isDeleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var album = new Album
        {
            UserId = user.Id,
            Title = title,
            Description = $"Mô tả riêng: {title}",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? now : null
        };

        dbContext.Albums.Add(album);
        await dbContext.SaveChangesAsync();
        return album;
    }

    private async Task<MemoryImage> CreateImageAsync(Memory memory)
    {
        var imagePath = $"uploads/tests/{Guid.NewGuid():N}.png";
        var filePath = ResolveImagePath(imagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, [137, 80, 78, 71, 13, 10, 26, 10]);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var image = new MemoryImage
        {
            MemoryId = memory.Id,
            ImagePath = imagePath,
            OriginalFileName = "private-test.png",
            UploadedAt = DateTime.UtcNow
        };

        dbContext.MemoryImages.Add(image);
        await dbContext.SaveChangesAsync();
        return image;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(TestUser user)
    {
        var client = CreateClient();
        using var loginPageResponse = await client.GetAsync("/Account/Login");
        var loginPage = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPage);

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

    private async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/Settings/EditProfile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractAntiForgeryToken(await response.Content.ReadAsStringAsync());
    }

    private async Task<bool> IsMemoryDeletedAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Memories.AnyAsync(memory => memory.Id == memoryId && memory.IsDeleted);
    }

    private async Task SoftDeleteMemoryAsync(int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = await dbContext.Memories.SingleAsync(item => item.Id == memoryId);

        memory.IsDeleted = true;
        memory.DeletedAt = DateTime.UtcNow;
        memory.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task<bool> IsAlbumDeletedAsync(int albumId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Albums.AnyAsync(album => album.Id == albumId && album.IsDeleted);
    }

    private async Task<bool> AlbumContainsMemoryAsync(int albumId, int memoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.AlbumMemories.AnyAsync(item =>
            item.AlbumId == albumId && item.MemoryId == memoryId);
    }

    private string ResolveImagePath(string imagePath)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<MemoLens.Services.IImageStorageService>();
        return storage.ResolveImagePath(imagePath)
            ?? throw new InvalidOperationException("The test image path could not be resolved.");
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
        return System.Net.WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static void AssertLoginRedirect(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.OriginalString);
    }

    private sealed record TestUser(string Id, string Email);
}
