using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public sealed class AlbumApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Password = "MemoLens1";
    private readonly CustomWebApplicationFactory _factory;

    public AlbumApiIntegrationTests(CustomWebApplicationFactory factory)
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
    [InlineData("GET", "/api/v1/albums")]
    [InlineData("GET", "/api/v1/albums/999999")]
    [InlineData("POST", "/api/v1/albums")]
    [InlineData("PUT", "/api/v1/albums/999999")]
    [InlineData("DELETE", "/api/v1/albums/999999")]
    [InlineData("POST", "/api/v1/albums/999999/restore")]
    [InlineData("POST", "/api/v1/albums/999999/memories")]
    [InlineData("DELETE", "/api/v1/albums/999999/memories/999999")]
    [InlineData("PUT", "/api/v1/albums/999999/cover")]
    [InlineData("DELETE", "/api/v1/albums/999999/cover")]
    public async Task AllAlbumEndpoints_WithoutBearerToken_ReturnUnauthorized(string method, string path)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method switch
            {
                "PUT" => JsonContent.Create(new { title = "Bộ sưu tập" }),
                "POST" when path.EndsWith("/memories", StringComparison.Ordinal) =>
                    JsonContent.Create(new { memoryIds = new[] { 1 } }),
                "POST" when path == "/api/v1/albums" => JsonContent.Create(new { title = "Bộ sưu tập" }),
                _ => null
            }
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("success", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_TrimsFieldsUsesJwtOwnerAndDoesNotExposePrivateData()
    {
        var owner = await CreateUserAsync("Owner");
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.PostAsJsonAsync("/api/v1/albums", new
        {
            title = "  Những chuyến đi  ",
            description = "  Các ký ức riêng.  "
        });
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Những chuyến đi", data.GetProperty("title").GetString());
        Assert.Equal("Các ký ức riêng.", data.GetProperty("description").GetString());
        Assert.Equal(0, data.GetProperty("memoryCount").GetInt32());
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("imagePath", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("coverImagePath", body, StringComparison.OrdinalIgnoreCase);

        var albumId = data.GetProperty("id").GetInt32();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var album = await dbContext.Albums.Include(item => item.AlbumMemories).SingleAsync(item => item.Id == albumId);
        Assert.Equal(owner.Id, album.UserId);
        Assert.Empty(album.AlbumMemories);
    }

    [Fact]
    public async Task Create_WithInitialMemories_PersistsAlbumAndOwnedActiveMembershipsAtomically()
    {
        var owner = await CreateUserAsync("Owner");
        var first = await CreateMemoryAsync(owner, "Một", DateTime.UtcNow.Date);
        var second = await CreateMemoryAsync(owner, "Hai", DateTime.UtcNow.Date.AddDays(-1));
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.PostAsJsonAsync("/api/v1/albums", new
        {
            title = "Tạo cùng kỷ niệm",
            memoryIds = new[] { first.Id, first.Id, second.Id }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, document.RootElement.GetProperty("data").GetProperty("memoryCount").GetInt32());

        var albumId = document.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await dbContext.AlbumMemories.CountAsync(item => item.AlbumId == albumId));
    }

    [Fact]
    public async Task Create_WithInvalidInitialMemory_DoesNotPersistAlbum()
    {
        var owner = await CreateUserAsync("Owner");
        var valid = await CreateMemoryAsync(owner, "Hợp lệ", DateTime.UtcNow.Date);
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.PostAsJsonAsync("/api/v1/albums", new
        {
            title = "Không được tạo",
            memoryIds = new[] { valid.Id, 999999 }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.Albums.AnyAsync(item => item.UserId == owner.Id && item.Title == "Không được tạo"));
    }

    [Fact]
    public async Task CreateAndUpdate_InvalidInput_ReturnStableValidationErrors()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Ban đầu");
        using var client = await CreateBearerClientAsync(owner);

        using var blankResponse = await client.PostAsJsonAsync("/api/v1/albums", new { title = "   " });
        using var longResponse = await client.PutAsJsonAsync($"/api/v1/albums/{album.Id}", new
        {
            title = new string('a', 101),
            description = new string('b', 501)
        });

        var blankBody = await blankResponse.Content.ReadAsStringAsync();
        var longBody = await longResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longResponse.StatusCode);
        Assert.Contains("errors", blankBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tiêu đề", blankBody, StringComparison.Ordinal);
        Assert.Contains("100", longBody, StringComparison.Ordinal);
        Assert.Contains("500", longBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_IsOwnerScopedSearchableSortablePaginatedAndUsesAuthorizedCoverUrl()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var newest = await CreateAlbumAsync(owner, "Du lịch mùa hè", "Đà Lạt", new DateTime(2026, 7, 3));
        await CreateAlbumAsync(owner, "Cà phê", "Cuối tuần", new DateTime(2026, 6, 2));
        var privateAlbum = await CreateAlbumAsync(other, "Riêng tư", "Không được lộ", new DateTime(2026, 7, 5));
        var memory = await CreateMemoryAsync(owner, "Đà Lạt", new DateTime(2026, 7, 2));
        var image = await CreateImageAsync(memory);
        await AddMembershipAsync(newest, memory);

        using var ownerClient = await CreateBearerClientAsync(owner);
        using var response = await ownerClient.GetAsync("/api/v1/albums?search=Đà&page=1&pageSize=1&sort=name");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");
        var item = data.GetProperty("items")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, data.GetProperty("totalItems").GetInt32());
        Assert.Equal(newest.Id, item.GetProperty("id").GetInt32());
        Assert.Equal(1, item.GetProperty("memoryCount").GetInt32());
        Assert.Equal(image.Id, item.GetProperty("coverImageId").GetInt32());
        Assert.Equal($"/api/v1/images/{image.Id}/content", item.GetProperty("coverImageUrl").GetString());
        Assert.DoesNotContain(privateAlbum.Title, body, StringComparison.Ordinal);
        Assert.DoesNotContain(image.ImagePath, body, StringComparison.Ordinal);

        using var cappedResponse = await ownerClient.GetAsync("/api/v1/albums?page=0&pageSize=999&sort=oldest");
        using var cappedDocument = JsonDocument.Parse(await cappedResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, cappedDocument.RootElement.GetProperty("data").GetProperty("page").GetInt32());
        Assert.Equal(100, cappedDocument.RootElement.GetProperty("data").GetProperty("pageSize").GetInt32());

        using var invalidSort = await ownerClient.GetAsync("/api/v1/albums?sort=random");
        Assert.Equal(HttpStatusCode.BadRequest, invalidSort.StatusCode);

        using var adminClient = await CreateBearerClientAsync(admin);
        using var adminResponse = await adminClient.GetAsync($"/api/v1/albums/{privateAlbum.Id}");
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Details_PaginatesNewestMemoriesHidesDeletedAndReturnsSafeSummaries()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Hành trình");
        var older = await CreateMemoryAsync(owner, "Cũ hơn", new DateTime(2026, 6, 1), ["cũ"]);
        var newer = await CreateMemoryAsync(owner, "Mới hơn", new DateTime(2026, 7, 1), ["mới"]);
        var deleted = await CreateMemoryAsync(owner, "Đã xóa", new DateTime(2026, 8, 1));
        var newerImage = await CreateImageAsync(newer);
        await AddMembershipAsync(album, older, new DateTime(2026, 6, 2));
        await AddMembershipAsync(album, newer, new DateTime(2026, 7, 2));
        await AddMembershipAsync(album, deleted, new DateTime(2026, 8, 2));
        await MarkMemoryDeletedAsync(deleted.Id);

        using var client = await CreateBearerClientAsync(owner);
        using var response = await client.GetAsync($"/api/v1/albums/{album.Id}?page=1&pageSize=1");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");
        var memories = data.GetProperty("memories");
        var item = memories.GetProperty("items")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, data.GetProperty("memoryCount").GetInt32());
        Assert.Equal(2, memories.GetProperty("totalItems").GetInt32());
        Assert.True(memories.GetProperty("hasNextPage").GetBoolean());
        Assert.Equal(newer.Id, item.GetProperty("id").GetInt32());
        Assert.Equal(1, item.GetProperty("imageCount").GetInt32());
        Assert.Equal("mới", item.GetProperty("tags")[0].GetString());
        Assert.Equal($"/api/v1/images/{newerImage.Id}/content", item.GetProperty("coverImageUrl").GetString());
        Assert.DoesNotContain(deleted.Title, body, StringComparison.Ordinal);
        Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("imagePath", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_ChangesOnlyAlbumFieldsAndRejectsCrossOwner()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var album = await CreateAlbumAsync(owner, "Cũ");
        var memory = await CreateMemoryAsync(owner, "Giữ lại", DateTime.UtcNow.Date);
        await AddMembershipAsync(album, memory);

        using var ownerClient = await CreateBearerClientAsync(owner);
        using var response = await ownerClient.PutAsJsonAsync($"/api/v1/albums/{album.Id}", new
        {
            title = "  Tên mới  ",
            description = "  Mô tả mới  ",
            memoryIds = Array.Empty<int>()
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updated = await dbContext.Albums.Include(item => item.AlbumMemories).SingleAsync(item => item.Id == album.Id);
            Assert.Equal("Tên mới", updated.Title);
            Assert.Equal("Mô tả mới", updated.Description);
            Assert.Single(updated.AlbumMemories);
        }

        using var otherClient = await CreateBearerClientAsync(other);
        using var forged = await otherClient.PutAsJsonAsync($"/api/v1/albums/{album.Id}", new { title = "Chiếm quyền" });
        Assert.Equal(HttpStatusCode.NotFound, forged.StatusCode);
    }

    [Fact]
    public async Task AddMemories_DeduplicatesAndIsIdempotent()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Album");
        var first = await CreateMemoryAsync(owner, "Một", DateTime.UtcNow.Date);
        var second = await CreateMemoryAsync(owner, "Hai", DateTime.UtcNow.Date.AddDays(-1));
        using var client = await CreateBearerClientAsync(owner);

        using var firstResponse = await client.PostAsJsonAsync($"/api/v1/albums/{album.Id}/memories", new
        {
            memoryIds = new[] { first.Id, first.Id, second.Id }
        });
        using var repeatedResponse = await client.PostAsJsonAsync($"/api/v1/albums/{album.Id}/memories", new
        {
            memoryIds = new[] { first.Id, second.Id }
        });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await dbContext.AlbumMemories.CountAsync(item => item.AlbumId == album.Id));
    }

    [Fact]
    public async Task AddMemories_InvalidForeignOrDeletedIdRollsBackEntireBatch()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var album = await CreateAlbumAsync(owner, "Album");
        var valid = await CreateMemoryAsync(owner, "Hợp lệ", DateTime.UtcNow.Date);
        var deleted = await CreateMemoryAsync(owner, "Đã xóa", DateTime.UtcNow.Date);
        var foreign = await CreateMemoryAsync(other, "Người khác", DateTime.UtcNow.Date);
        await MarkMemoryDeletedAsync(deleted.Id);
        using var client = await CreateBearerClientAsync(owner);

        foreach (var invalidId in new[] { 999999, foreign.Id, deleted.Id })
        {
            using var response = await client.PostAsJsonAsync($"/api/v1/albums/{album.Id}/memories", new
            {
                memoryIds = new[] { valid.Id, invalidId }
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.AlbumMemories.AnyAsync(item => item.AlbumId == album.Id));
    }

    [Fact]
    public async Task RemoveMemory_DeletesOnlyMembershipAndRepeatedCallReturnsNotFound()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Album");
        var memory = await CreateMemoryAsync(owner, "Kỷ niệm", DateTime.UtcNow.Date);
        var image = await CreateImageAsync(memory);
        await AddMembershipAsync(album, memory);
        using var client = await CreateBearerClientAsync(owner);

        using var response = await client.DeleteAsync($"/api/v1/albums/{album.Id}/memories/{memory.Id}");
        using var repeated = await client.DeleteAsync($"/api/v1/albums/{album.Id}/memories/{memory.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.Memories.AnyAsync(item => item.Id == memory.Id));
        Assert.True(await dbContext.MemoryImages.AnyAsync(item => item.Id == image.Id));
        Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));
    }

    [Fact]
    public async Task DeleteAndRestore_PreserveMembershipsAndKeepDeletedMemoriesHidden()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Khôi phục");
        var visible = await CreateMemoryAsync(owner, "Hiển thị", DateTime.UtcNow.Date);
        var deleted = await CreateMemoryAsync(owner, "Đang trong rác", DateTime.UtcNow.Date.AddDays(-1));
        var image = await CreateImageAsync(visible);
        await AddMembershipAsync(album, visible);
        await AddMembershipAsync(album, deleted);
        await MarkMemoryDeletedAsync(deleted.Id);
        using var client = await CreateBearerClientAsync(owner);

        using var deleteResponse = await client.DeleteAsync($"/api/v1/albums/{album.Id}");
        using var hiddenResponse = await client.GetAsync($"/api/v1/albums/{album.Id}");
        using var repeatedDelete = await client.DeleteAsync($"/api/v1/albums/{album.Id}");
        using var restoreResponse = await client.PostAsync($"/api/v1/albums/{album.Id}/restore", null);
        var restoreBody = await restoreResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeatedDelete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.Contains(visible.Title, restoreBody, StringComparison.Ordinal);
        Assert.DoesNotContain(deleted.Title, restoreBody, StringComparison.Ordinal);
        Assert.True(File.Exists(ResolveImagePath(image.ImagePath)));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await dbContext.AlbumMemories.CountAsync(item => item.AlbumId == album.Id));
    }

    [Fact]
    public async Task CoverOverride_UsesLatestAddedMemoryAndCanResetToAutomatic()
    {
        var owner = await CreateUserAsync("Owner");
        var album = await CreateAlbumAsync(owner, "Cover test");
        var earlierMemory = await CreateMemoryAsync(owner, "Earlier", DateTime.UtcNow.Date.AddDays(-1));
        var latestMemory = await CreateMemoryAsync(owner, "Latest", DateTime.UtcNow.Date);
        var earlierImage = await CreateImageAsync(earlierMemory);
        var latestImage = await CreateImageAsync(latestMemory);
        await AddMembershipAsync(album, earlierMemory, DateTime.UtcNow.AddMinutes(-2));
        await AddMembershipAsync(album, latestMemory, DateTime.UtcNow.AddMinutes(-1));

        using var client = await CreateBearerClientAsync(owner);
        using var automaticResponse = await client.GetAsync($"/api/v1/albums/{album.Id}");
        using var automaticDocument = JsonDocument.Parse(await automaticResponse.Content.ReadAsStringAsync());
        Assert.Equal(latestImage.Id, automaticDocument.RootElement.GetProperty("data").GetProperty("effectiveCoverImageId").GetInt32());

        using var setResponse = await client.PutAsJsonAsync($"/api/v1/albums/{album.Id}/cover", new { imageId = earlierImage.Id });
        using var setDocument = JsonDocument.Parse(await setResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        Assert.Equal(earlierImage.Id, setDocument.RootElement.GetProperty("data").GetProperty("manualCoverImageId").GetInt32());
        Assert.Equal(earlierImage.Id, setDocument.RootElement.GetProperty("data").GetProperty("effectiveCoverImageId").GetInt32());

        using var resetResponse = await client.DeleteAsync($"/api/v1/albums/{album.Id}/cover");
        using var resetDocument = JsonDocument.Parse(await resetResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(JsonValueKind.Null, resetDocument.RootElement.GetProperty("data").GetProperty("manualCoverImageId").ValueKind);
        Assert.Equal(latestImage.Id, resetDocument.RootElement.GetProperty("data").GetProperty("effectiveCoverImageId").GetInt32());
    }

    [Fact]
    public async Task OtherUserAndAdmin_CannotMutatePrivateAlbumOrMembership()
    {
        var owner = await CreateUserAsync("Owner");
        var other = await CreateUserAsync("Other");
        var admin = await CreateUserAsync("Admin", addAdminRole: true);
        var album = await CreateAlbumAsync(owner, "Riêng tư");
        var memory = await CreateMemoryAsync(owner, "Riêng tư", DateTime.UtcNow.Date);
        await AddMembershipAsync(album, memory);

        foreach (var attacker in new[] { other, admin })
        {
            using var client = await CreateBearerClientAsync(attacker);
            using var details = await client.GetAsync($"/api/v1/albums/{album.Id}");
            using var update = await client.PutAsJsonAsync($"/api/v1/albums/{album.Id}", new { title = "Chiếm quyền" });
            using var delete = await client.DeleteAsync($"/api/v1/albums/{album.Id}");
            using var restore = await client.PostAsync($"/api/v1/albums/{album.Id}/restore", null);
            using var add = await client.PostAsJsonAsync($"/api/v1/albums/{album.Id}/memories", new { memoryIds = new[] { memory.Id } });
            using var remove = await client.DeleteAsync($"/api/v1/albums/{album.Id}/memories/{memory.Id}");
            using var setCover = await client.PutAsJsonAsync($"/api/v1/albums/{album.Id}/cover", new { imageId = 1 });
            using var resetCover = await client.DeleteAsync($"/api/v1/albums/{album.Id}/cover");

            Assert.All(new[] { details, update, delete, restore, add, remove, setCover, resetCover }, response =>
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unchanged = await dbContext.Albums.SingleAsync(item => item.Id == album.Id);
        Assert.False(unchanged.IsDeleted);
        Assert.Single(await dbContext.AlbumMemories.Where(item => item.AlbumId == album.Id).ToListAsync());
    }

    private async Task<TestUser> CreateUserAsync(string displayName, bool addAdminRole = false)
    {
        var email = $"album-api-{Guid.NewGuid():N}@example.test";
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

        return new TestUser(user.Id);
    }

    private async Task<Album> CreateAlbumAsync(
        TestUser user,
        string title,
        string? description = null,
        DateTime? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var timestamp = createdAt ?? DateTime.UtcNow;
        var album = new Album
        {
            UserId = user.Id,
            Title = title,
            Description = description,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        dbContext.Albums.Add(album);
        await dbContext.SaveChangesAsync();
        return album;
    }

    private async Task<Memory> CreateMemoryAsync(
        TestUser user,
        string title,
        DateTime memoryDate,
        IReadOnlyList<string>? tags = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memory = new Memory
        {
            UserId = user.Id,
            Title = title,
            Story = $"Câu chuyện: {title}",
            Feeling = "Bình yên",
            MemoryDate = memoryDate.Date,
            Location = "Hà Nội",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        foreach (var tagName in tags ?? [])
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
            OriginalFileName = "album-cover.png",
            UploadedAt = DateTime.UtcNow
        };
        dbContext.MemoryImages.Add(image);
        await dbContext.SaveChangesAsync();
        return image;
    }

    private async Task AddMembershipAsync(Album album, Memory memory, DateTime? addedAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.AlbumMemories.Add(new AlbumMemory
        {
            AlbumId = album.Id,
            MemoryId = memory.Id,
            AddedAt = addedAt ?? DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private async Task MarkMemoryDeletedAsync(int memoryId)
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await tokenService.GenerateAccessTokenAsync(applicationUser));
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

    private sealed record TestUser(string Id);
}
