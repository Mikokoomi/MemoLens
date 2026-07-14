using System.Security.Claims;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Api.Albums;
using MemoLens.Models.Api.Images;
using MemoLens.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1/albums")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class AlbumsController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;
    private const int StoryPreviewLength = 240;

    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorageService;

    public AlbumsController(ApplicationDbContext context, IImageStorageService imageStorageService)
    {
        _context = context;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AlbumListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResponse<AlbumListItemResponse>>>> GetAll(
        [FromQuery] AlbumListQuery queryParameters)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var validationErrors = ValidateSort(queryParameters.Sort);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var pageSize = NormalizePageSize(queryParameters.PageSize);
        var page = NormalizePage(queryParameters.Page, pageSize);
        var search = CleanOptionalText(queryParameters.Search);

        IQueryable<Album> albums = _context.Albums
            .AsNoTracking()
            .Where(album => album.UserId == userId && !album.IsDeleted);

        if (search is not null)
        {
            albums = albums.Where(album =>
                album.Title.Contains(search) ||
                (album.Description != null && album.Description.Contains(search)));
        }

        albums = queryParameters.Sort?.Trim().ToLowerInvariant() switch
        {
            "oldest" => albums.OrderBy(album => album.CreatedAt).ThenBy(album => album.Id),
            "name" => albums.OrderBy(album => album.Title).ThenBy(album => album.Id),
            _ => albums.OrderByDescending(album => album.CreatedAt).ThenByDescending(album => album.Id)
        };

        var totalItems = await albums.CountAsync();
        var totalPages = CalculateTotalPages(totalItems, pageSize);
        var rows = await albums
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(album => new AlbumListRow
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                MemoryCount = album.AlbumMemories.Count(albumMemory =>
                    albumMemory.Memory.UserId == userId && !albumMemory.Memory.IsDeleted),
                CoverCandidates = album.AlbumMemories
                    .Where(albumMemory => albumMemory.Memory.UserId == userId && !albumMemory.Memory.IsDeleted)
                    .OrderBy(albumMemory => albumMemory.AddedAt)
                    .ThenBy(albumMemory => albumMemory.MemoryId)
                    .SelectMany(albumMemory => albumMemory.Memory.Images
                        .OrderBy(image => image.UploadedAt)
                        .ThenBy(image => image.Id))
                    .Take(10)
                    .Select(image => new ImageCandidate
                    {
                        Id = image.Id,
                        ImagePath = image.ImagePath
                    })
                    .ToList(),
                CreatedAt = album.CreatedAt,
                UpdatedAt = album.UpdatedAt
            })
            .ToListAsync();

        var items = rows.Select(row =>
        {
            var cover = FindFirstAccessibleImage(row.CoverCandidates);
            return new AlbumListItemResponse
            {
                Id = row.Id,
                Title = row.Title,
                Description = row.Description,
                MemoryCount = row.MemoryCount,
                CoverImageId = cover?.Id,
                CoverImageUrl = cover is null ? null : MemoryImageApiRoutes.Content(cover.Id),
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            };
        }).ToList();

        return Ok(new ApiResponse<PagedResponse<AlbumListItemResponse>>
        {
            Success = true,
            Message = "Lấy danh sách bộ sưu tập thành công.",
            Data = CreatePage(items, page, pageSize, totalItems, totalPages)
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> GetById(
        int id,
        [FromQuery] AlbumDetailsQuery queryParameters)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var pageSize = NormalizePageSize(queryParameters.PageSize);
        var details = await BuildDetailsResponseAsync(
            id,
            userId,
            NormalizePage(queryParameters.Page, pageSize),
            pageSize);
        if (details is null)
        {
            return AlbumNotFound();
        }

        return Ok(new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = "Lấy chi tiết bộ sưu tập thành công.",
            Data = details
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> Create([FromBody] CreateAlbumRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var validationErrors = ValidateAlbumRequest(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var now = DateTime.UtcNow;
        var album = new Album
        {
            UserId = userId,
            Title = request.Title!.Trim(),
            Description = CleanOptionalText(request.Description),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        var details = await BuildDetailsResponseAsync(album.Id, userId, 1, DefaultPageSize);
        return CreatedAtAction(nameof(GetById), new { id = album.Id }, new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = "Tạo bộ sưu tập thành công.",
            Data = details
        });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> Update(
        int id,
        [FromBody] UpdateAlbumRequest request)
    {
        var validationErrors = ValidateAlbumRequest(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var album = await FindOwnedAlbumAsync(id, includeDeleted: false);
        if (album is null)
        {
            return AlbumNotFound();
        }

        album.Title = request.Title!.Trim();
        album.Description = CleanOptionalText(request.Description);
        album.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var details = await BuildDetailsResponseAsync(album.Id, album.UserId, 1, DefaultPageSize);
        return Ok(new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = "Cập nhật bộ sưu tập thành công.",
            Data = details
        });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var album = await FindOwnedAlbumAsync(id, includeDeleted: false);
        if (album is null)
        {
            return AlbumNotFound();
        }

        var now = DateTime.UtcNow;
        album.IsDeleted = true;
        album.DeletedAt = now;
        album.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Bộ sưu tập đã được chuyển vào thùng rác."
        });
    }

    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> Restore(int id)
    {
        var album = await FindOwnedAlbumAsync(id, includeDeleted: true);
        if (album is null || !album.IsDeleted)
        {
            return AlbumNotFound();
        }

        album.IsDeleted = false;
        album.DeletedAt = null;
        album.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var details = await BuildDetailsResponseAsync(album.Id, album.UserId, 1, DefaultPageSize);
        return Ok(new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = "Khôi phục bộ sưu tập thành công.",
            Data = details
        });
    }

    [HttpPost("{id:int}/memories")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> AddMemories(
        int id,
        [FromBody] AddAlbumMemoriesRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        if (request.MemoryIds is null || request.MemoryIds.Count == 0)
        {
            return ValidationFailure(new Dictionary<string, string[]>
            {
                ["memoryIds"] = ["Vui lòng chọn ít nhất một kỷ niệm."]
            });
        }

        var album = await _context.Albums
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId && !item.IsDeleted);
        if (album is null)
        {
            return AlbumNotFound();
        }

        var memoryIds = request.MemoryIds.Distinct().ToList();
        if (memoryIds.Any(memoryId => memoryId <= 0))
        {
            return MemoryNotFound();
        }

        var validMemoryIds = await _context.Memories
            .AsNoTracking()
            .Where(memory =>
                memoryIds.Contains(memory.Id) &&
                memory.UserId == userId &&
                !memory.IsDeleted)
            .Select(memory => memory.Id)
            .ToListAsync();
        if (validMemoryIds.Count != memoryIds.Count)
        {
            return MemoryNotFound();
        }

        var existingIds = await _context.AlbumMemories
            .AsNoTracking()
            .Where(item => item.AlbumId == album.Id && memoryIds.Contains(item.MemoryId))
            .Select(item => item.MemoryId)
            .ToListAsync();
        var newIds = memoryIds.Except(existingIds).ToList();

        if (newIds.Count > 0)
        {
            var now = DateTime.UtcNow;
            _context.AlbumMemories.AddRange(newIds.Select(memoryId => new AlbumMemory
            {
                AlbumId = album.Id,
                MemoryId = memoryId,
                AddedAt = now
            }));
            album.UpdatedAt = now;
            await _context.SaveChangesAsync();
        }

        var details = await BuildDetailsResponseAsync(album.Id, userId, 1, DefaultPageSize);
        return Ok(new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = newIds.Count == 0
                ? "Các kỷ niệm đã có trong bộ sưu tập."
                : "Thêm kỷ niệm vào bộ sưu tập thành công.",
            Data = details
        });
    }

    [HttpDelete("{id:int}/memories/{memoryId:int}")]
    [ProducesResponseType(typeof(ApiResponse<AlbumDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AlbumDetailsResponse>>> RemoveMemory(int id, int memoryId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var album = await _context.Albums
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId && !item.IsDeleted);
        if (album is null)
        {
            return AlbumNotFound();
        }

        var membership = await _context.AlbumMemories
            .Include(item => item.Memory)
            .FirstOrDefaultAsync(item =>
                item.AlbumId == album.Id &&
                item.MemoryId == memoryId &&
                item.Memory.UserId == userId);
        if (membership is null)
        {
            return MemoryNotFound();
        }

        _context.AlbumMemories.Remove(membership);
        album.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var details = await BuildDetailsResponseAsync(album.Id, userId, 1, DefaultPageSize);
        return Ok(new ApiResponse<AlbumDetailsResponse>
        {
            Success = true,
            Message = "Đã gỡ kỷ niệm khỏi bộ sưu tập.",
            Data = details
        });
    }

    private async Task<Album?> FindOwnedAlbumAsync(int id, bool includeDeleted)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        var query = _context.Albums.Where(album => album.Id == id && album.UserId == userId);
        if (!includeDeleted)
        {
            query = query.Where(album => !album.IsDeleted);
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<AlbumDetailsResponse?> BuildDetailsResponseAsync(
        int albumId,
        string userId,
        int page,
        int pageSize)
    {
        var album = await _context.Albums
            .AsNoTracking()
            .Where(item => item.Id == albumId && item.UserId == userId && !item.IsDeleted)
            .Select(item => new AlbumDetailsRow
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();
        if (album is null)
        {
            return null;
        }

        var memberships = _context.AlbumMemories
            .AsNoTracking()
            .Where(item =>
                item.AlbumId == albumId &&
                item.Memory.UserId == userId &&
                !item.Memory.IsDeleted);

        var totalItems = await memberships.CountAsync();
        var totalPages = CalculateTotalPages(totalItems, pageSize);
        var rows = await memberships
            .OrderByDescending(item => item.Memory.MemoryDate)
            .ThenByDescending(item => item.Memory.CreatedAt)
            .ThenByDescending(item => item.MemoryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AlbumMemoryRow
            {
                Id = item.Memory.Id,
                Title = item.Memory.Title,
                Story = item.Memory.Story,
                Feeling = item.Memory.Feeling,
                MemoryDate = item.Memory.MemoryDate,
                Location = item.Memory.Location,
                Tags = item.Memory.MemoryTags
                    .OrderBy(memoryTag => memoryTag.Tag.Name)
                    .Select(memoryTag => memoryTag.Tag.Name)
                    .ToList(),
                ImageCount = item.Memory.Images.Count,
                CoverCandidates = item.Memory.Images
                    .OrderBy(image => image.UploadedAt)
                    .ThenBy(image => image.Id)
                    .Select(image => new ImageCandidate
                    {
                        Id = image.Id,
                        ImagePath = image.ImagePath
                    })
                    .ToList(),
                AddedAt = item.AddedAt
            })
            .ToListAsync();

        var coverCandidates = await memberships
            .OrderBy(item => item.AddedAt)
            .ThenBy(item => item.MemoryId)
            .SelectMany(item => item.Memory.Images
                .OrderBy(image => image.UploadedAt)
                .ThenBy(image => image.Id))
            .Take(10)
            .Select(image => new ImageCandidate
            {
                Id = image.Id,
                ImagePath = image.ImagePath
            })
            .ToListAsync();
        var albumCover = FindFirstAccessibleImage(coverCandidates);

        var memories = rows.Select(row =>
        {
            var cover = FindFirstAccessibleImage(row.CoverCandidates);
            return new AlbumMemorySummaryResponse
            {
                Id = row.Id,
                Title = row.Title,
                ShortStoryPreview = CreateStoryPreview(row.Story),
                Feeling = row.Feeling,
                MemoryDate = row.MemoryDate,
                Location = row.Location,
                Tags = row.Tags,
                ImageCount = row.ImageCount,
                CoverImageId = cover?.Id,
                CoverImageUrl = cover is null ? null : MemoryImageApiRoutes.Content(cover.Id),
                AddedAt = row.AddedAt
            };
        }).ToList();

        return new AlbumDetailsResponse
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description,
            MemoryCount = totalItems,
            CoverImageId = albumCover?.Id,
            CoverImageUrl = albumCover is null ? null : MemoryImageApiRoutes.Content(albumCover.Id),
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt,
            Memories = CreatePage(memories, page, pageSize, totalItems, totalPages)
        };
    }

    private ImageCandidate? FindFirstAccessibleImage(IEnumerable<ImageCandidate> candidates)
    {
        return candidates.FirstOrDefault(candidate =>
        {
            var physicalPath = _imageStorageService.ResolveImagePath(candidate.ImagePath);
            return physicalPath is not null && System.IO.File.Exists(physicalPath);
        });
    }

    private static PagedResponse<T> CreatePage<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems,
        int totalPages)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = page > 1 && totalItems > 0,
            HasNextPage = totalPages > 0 && page < totalPages
        };
    }

    private static Dictionary<string, string[]> ValidateAlbumRequest(CreateAlbumRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Tiêu đề là bắt buộc."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateSort(string? sort)
    {
        var cleanedSort = CleanOptionalText(sort)?.ToLowerInvariant();
        if (cleanedSort is null || cleanedSort is "newest" or "oldest" or "name")
        {
            return [];
        }

        return new Dictionary<string, string[]>
        {
            ["sort"] = ["Cách sắp xếp không hợp lệ. Hãy dùng newest, oldest hoặc name."]
        };
    }

    private static int NormalizePage(int? page, int pageSize)
    {
        if (!page.HasValue || page.Value < 1)
        {
            return 1;
        }

        var maximumPage = ((long)int.MaxValue / pageSize) + 1;
        return (int)Math.Min(page.Value, maximumPage);
    }

    private static int NormalizePageSize(int? pageSize) => Math.Clamp(pageSize ?? DefaultPageSize, 1, MaximumPageSize);

    private static int CalculateTotalPages(int totalItems, int pageSize) =>
        totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

    private static string? CleanOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? CreateStoryPreview(string? story)
    {
        var cleanedStory = CleanOptionalText(story);
        if (cleanedStory is null || cleanedStory.Length <= StoryPreviewLength)
        {
            return cleanedStory;
        }

        return $"{cleanedStory[..StoryPreviewLength]}...";
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private BadRequestObjectResult ValidationFailure(Dictionary<string, string[]> errors) =>
        BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Dữ liệu gửi lên chưa hợp lệ.",
            Errors = errors
        });

    private UnauthorizedObjectResult InvalidBearerToken() =>
        Unauthorized(new ApiResponse
        {
            Success = false,
            Message = "Bearer token không hợp lệ."
        });

    private NotFoundObjectResult AlbumNotFound() =>
        NotFound(new ApiResponse
        {
            Success = false,
            Message = "Không tìm thấy bộ sưu tập."
        });

    private NotFoundObjectResult MemoryNotFound() =>
        NotFound(new ApiResponse
        {
            Success = false,
            Message = "Không tìm thấy kỷ niệm phù hợp."
        });

    private sealed class AlbumListRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int MemoryCount { get; init; }
        public IReadOnlyList<ImageCandidate> CoverCandidates { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class AlbumDetailsRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class AlbumMemoryRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Story { get; init; }
        public string Feeling { get; init; } = string.Empty;
        public DateTime MemoryDate { get; init; }
        public string? Location { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = [];
        public int ImageCount { get; init; }
        public IReadOnlyList<ImageCandidate> CoverCandidates { get; init; } = [];
        public DateTime AddedAt { get; init; }
    }

    private sealed class ImageCandidate
    {
        public int Id { get; init; }
        public string ImagePath { get; init; } = string.Empty;
    }
}
