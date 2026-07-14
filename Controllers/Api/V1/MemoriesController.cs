using System.Security.Claims;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Api.Memories;
using MemoLens.Models.Memories;
using MemoLens.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1/memories")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MemoriesController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;
    private const int StoryPreviewLength = 240;

    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MemoriesController(
        ApplicationDbContext context,
        IImageStorageService imageStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _imageStorageService = imageStorageService;
        _userManager = userManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MemoryListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResponse<MemoryListItemResponse>>>> GetAll(
        [FromQuery] MemoryListQuery queryParameters)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var validationErrors = ValidateListQuery(queryParameters);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var pageSize = NormalizePageSize(queryParameters.PageSize);
        var page = NormalizePage(queryParameters.Page, pageSize);
        var search = CleanOptionalText(queryParameters.Search);
        var feeling = CleanOptionalText(queryParameters.Feeling);
        var tag = CleanOptionalText(queryParameters.Tag);
        var from = queryParameters.From?.Date;
        var to = queryParameters.To?.Date;

        IQueryable<Memory> memories = _context.Memories
            .AsNoTracking()
            .Where(memory => memory.UserId == userId && !memory.IsDeleted);

        if (search is not null)
        {
            memories = memories.Where(memory =>
                memory.Title.Contains(search) ||
                (memory.Story != null && memory.Story.Contains(search)) ||
                (memory.Location != null && memory.Location.Contains(search)) ||
                memory.MemoryTags.Any(memoryTag => memoryTag.Tag.Name.Contains(search)));
        }

        if (feeling is not null)
        {
            memories = memories.Where(memory => memory.Feeling == feeling);
        }

        if (tag is not null)
        {
            memories = memories.Where(memory =>
                memory.MemoryTags.Any(memoryTag => memoryTag.Tag.Name == tag));
        }

        if (from.HasValue || to.HasValue)
        {
            if (from.HasValue)
            {
                memories = memories.Where(memory => memory.MemoryDate >= from.Value);
            }

            if (to.HasValue)
            {
                memories = memories.Where(memory => memory.MemoryDate <= to.Value);
            }
        }
        else if (queryParameters.Year.HasValue)
        {
            memories = memories.Where(memory => memory.MemoryDate.Year == queryParameters.Year.Value);

            if (queryParameters.Month.HasValue)
            {
                memories = memories.Where(memory => memory.MemoryDate.Month == queryParameters.Month.Value);
            }
        }

        var isOldestFirst = string.Equals(queryParameters.Sort, "oldest", StringComparison.OrdinalIgnoreCase);
        memories = isOldestFirst
            ? memories.OrderBy(memory => memory.MemoryDate).ThenBy(memory => memory.CreatedAt)
            : memories.OrderByDescending(memory => memory.MemoryDate).ThenByDescending(memory => memory.CreatedAt);

        var totalItems = await memories.CountAsync();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var rows = await memories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(memory => new MemoryListRow
            {
                Id = memory.Id,
                Title = memory.Title,
                Story = memory.Story,
                Feeling = memory.Feeling,
                MemoryDate = memory.MemoryDate,
                Location = memory.Location,
                Tags = memory.MemoryTags
                    .OrderBy(memoryTag => memoryTag.Tag.Name)
                    .Select(memoryTag => memoryTag.Tag.Name)
                    .ToList(),
                ImageCount = memory.Images.Count,
                CoverImageId = memory.Images
                    .OrderBy(image => image.UploadedAt)
                    .Select(image => (int?)image.Id)
                    .FirstOrDefault(),
                CoverImagePath = memory.Images
                    .OrderBy(image => image.UploadedAt)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault(),
                CreatedAt = memory.CreatedAt,
                UpdatedAt = memory.UpdatedAt
            })
            .ToListAsync();

        var items = rows.Select(row => new MemoryListItemResponse
        {
            Id = row.Id,
            Title = row.Title,
            ShortStoryPreview = CreateStoryPreview(row.Story),
            Feeling = row.Feeling,
            MemoryDate = row.MemoryDate,
            Location = row.Location,
            Tags = row.Tags,
            ImageCount = row.ImageCount,
            CoverImageId = IsAccessibleImage(row.CoverImagePath) ? row.CoverImageId : null,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        }).ToList();

        return Ok(new ApiResponse<PagedResponse<MemoryListItemResponse>>
        {
            Success = true,
            Message = "Lấy danh sách kỷ niệm thành công.",
            Data = new PagedResponse<MemoryListItemResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPreviousPage = page > 1 && totalItems > 0,
                HasNextPage = totalPages > 0 && page < totalPages
            }
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MemoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MemoryDetailsResponse>>> GetById(int id)
    {
        var memory = await FindOwnedMemoryAsync(id, includeDeleted: false, asTracking: false);
        if (memory is null)
        {
            return NotFoundResponse();
        }

        return Ok(new ApiResponse<MemoryDetailsResponse>
        {
            Success = true,
            Message = "Lấy chi tiết kỷ niệm thành công.",
            Data = ToDetailsResponse(memory)
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MemoryDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<MemoryDetailsResponse>>> Create([FromBody] CreateMemoryRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var validationErrors = ValidateMemoryRequest(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var now = DateTime.UtcNow;
        var memory = new Memory
        {
            UserId = userId,
            Title = request.Title!.Trim(),
            Story = CleanOptionalText(request.Story),
            Feeling = request.Feeling!.Trim(),
            MemoryDate = request.MemoryDate!.Value.Date,
            Location = CleanOptionalText(request.Location),
            CreatedAt = now,
            UpdatedAt = now
        };

        await AddTagsAsync(memory, NormalizeTags(request.Tags));
        _context.Memories.Add(memory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = memory.Id }, new ApiResponse<MemoryDetailsResponse>
        {
            Success = true,
            Message = "Tạo kỷ niệm thành công.",
            Data = ToDetailsResponse(memory)
        });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MemoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MemoryDetailsResponse>>> Update(int id, [FromBody] UpdateMemoryRequest request)
    {
        var validationErrors = ValidateMemoryRequest(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var memory = await FindOwnedMemoryAsync(id, includeDeleted: false, asTracking: true);
        if (memory is null)
        {
            return NotFoundResponse();
        }

        memory.Title = request.Title!.Trim();
        memory.Story = CleanOptionalText(request.Story);
        memory.Feeling = request.Feeling!.Trim();
        memory.MemoryDate = request.MemoryDate!.Value.Date;
        memory.Location = CleanOptionalText(request.Location);
        memory.UpdatedAt = DateTime.UtcNow;

        _context.MemoryTags.RemoveRange(memory.MemoryTags);
        memory.MemoryTags.Clear();
        await AddTagsAsync(memory, NormalizeTags(request.Tags));
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<MemoryDetailsResponse>
        {
            Success = true,
            Message = "Cập nhật kỷ niệm thành công.",
            Data = ToDetailsResponse(memory)
        });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var memory = await FindOwnedMemoryAsync(id, includeDeleted: false, asTracking: true);
        if (memory is null)
        {
            return NotFoundResponse();
        }

        var now = DateTime.UtcNow;
        memory.IsDeleted = true;
        memory.DeletedAt = now;
        memory.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Kỷ niệm đã được chuyển vào thùng rác."
        });
    }

    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<MemoryDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MemoryDetailsResponse>>> Restore(int id)
    {
        var memory = await FindOwnedMemoryAsync(id, includeDeleted: true, asTracking: true);
        if (memory is null || !memory.IsDeleted)
        {
            return NotFoundResponse();
        }

        memory.IsDeleted = false;
        memory.DeletedAt = null;
        memory.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<MemoryDetailsResponse>
        {
            Success = true,
            Message = "Khôi phục kỷ niệm thành công.",
            Data = ToDetailsResponse(memory)
        });
    }

    private async Task<Memory?> FindOwnedMemoryAsync(int id, bool includeDeleted, bool asTracking)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        var query = _context.Memories
            .Include(memory => memory.Images)
            .Include(memory => memory.MemoryTags)
                .ThenInclude(memoryTag => memoryTag.Tag)
            .Where(memory => memory.Id == id && memory.UserId == userId);

        if (!includeDeleted)
        {
            query = query.Where(memory => !memory.IsDeleted);
        }

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task AddTagsAsync(Memory memory, IReadOnlyList<string> tagNames)
    {
        foreach (var tagName in tagNames)
        {
            var normalizedName = tagName.ToUpperInvariant();
            var tag = await _context.Tags.FirstOrDefaultAsync(existingTag =>
                existingTag.Name.ToUpper() == normalizedName);

            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                _context.Tags.Add(tag);
            }

            memory.MemoryTags.Add(new MemoryTag
            {
                Memory = memory,
                Tag = tag
            });
        }
    }

    private static Dictionary<string, string[]> ValidateMemoryRequest(CreateMemoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Tiêu đề là bắt buộc."];
        }

        if (string.IsNullOrWhiteSpace(request.Feeling) ||
            !MemoryFeelingOptions.All.Contains(request.Feeling.Trim(), StringComparer.Ordinal))
        {
            errors["feeling"] = ["Cảm xúc không hợp lệ."];
        }

        if (!request.MemoryDate.HasValue)
        {
            errors["memoryDate"] = ["Ngày kỷ niệm là bắt buộc."];
        }

        var invalidTag = request.Tags?
            .FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag) && tag.Trim().Length > 50);
        if (invalidTag is not null)
        {
            errors["tags"] = ["Mỗi thẻ không được dài quá 50 ký tự."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateListQuery(MemoryListQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        var feeling = CleanOptionalText(query.Feeling);
        if (feeling is not null && !MemoryFeelingOptions.All.Contains(feeling, StringComparer.Ordinal))
        {
            errors["feeling"] = ["Cảm xúc không hợp lệ."];
        }

        if (query.From.HasValue && query.To.HasValue && query.From.Value.Date > query.To.Value.Date)
        {
            errors["from"] = ["Từ ngày không được lớn hơn đến ngày."];
        }

        if (query.Month.HasValue && (query.Month.Value < 1 || query.Month.Value > 12))
        {
            errors["month"] = ["Tháng phải nằm trong khoảng từ 1 đến 12."];
        }

        if (query.Year.HasValue && (query.Year.Value < 1 || query.Year.Value > 9999))
        {
            errors["year"] = ["Năm không hợp lệ."];
        }

        return errors;
    }

    private BadRequestObjectResult ValidationFailure(Dictionary<string, string[]> errors)
    {
        return BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Dữ liệu gửi lên chưa hợp lệ.",
            Errors = errors
        });
    }

    private UnauthorizedObjectResult InvalidBearerToken()
    {
        return Unauthorized(new ApiResponse
        {
            Success = false,
            Message = "Bearer token không hợp lệ."
        });
    }

    private NotFoundObjectResult NotFoundResponse()
    {
        return NotFound(new ApiResponse
        {
            Success = false,
            Message = "Không tìm thấy kỷ niệm."
        });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private bool IsAccessibleImage(string? imagePath)
    {
        var filePath = imagePath is null ? null : _imageStorageService.ResolveImagePath(imagePath);
        return filePath is not null && System.IO.File.Exists(filePath);
    }

    private static int NormalizePageSize(int? pageSize)
    {
        if (!pageSize.HasValue || pageSize.Value < 1)
        {
            return DefaultPageSize;
        }

        return Math.Min(pageSize.Value, MaximumPageSize);
    }

    private static int NormalizePage(int? page, int pageSize)
    {
        if (!page.HasValue || page.Value < 1)
        {
            return 1;
        }

        var maximumPage = (int.MaxValue / pageSize) + 1;
        return Math.Min(page.Value, maximumPage);
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string?>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? CreateStoryPreview(string? story)
    {
        var cleanedStory = CleanOptionalText(story);
        if (cleanedStory is null || cleanedStory.Length <= StoryPreviewLength)
        {
            return cleanedStory;
        }

        return $"{cleanedStory[..StoryPreviewLength]}...";
    }

    private static MemoryDetailsResponse ToDetailsResponse(Memory memory)
    {
        return new MemoryDetailsResponse
        {
            Id = memory.Id,
            Title = memory.Title,
            Story = memory.Story,
            Feeling = memory.Feeling,
            MemoryDate = memory.MemoryDate,
            Location = memory.Location,
            Tags = memory.MemoryTags
                .OrderBy(memoryTag => memoryTag.Tag.Name)
                .Select(memoryTag => memoryTag.Tag.Name)
                .ToList(),
            Images = memory.Images
                .OrderBy(image => image.UploadedAt)
                .Select(image => new MemoryImageResponse
                {
                    Id = image.Id,
                    OriginalFileName = image.OriginalFileName,
                    UploadedAt = image.UploadedAt,
                    ContentUrl = $"/Images/MemoryImage/{image.Id}"
                })
                .ToList(),
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt
        };
    }

    private sealed class MemoryListRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Story { get; init; }
        public string Feeling { get; init; } = string.Empty;
        public DateTime MemoryDate { get; init; }
        public string? Location { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = [];
        public int ImageCount { get; init; }
        public int? CoverImageId { get; init; }
        public string? CoverImagePath { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
