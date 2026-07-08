using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Memories;
using MemoLens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

[Authorize]
public class MemoriesController : Controller
{
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

    public async Task<IActionResult> Index(
        string? search,
        string? feeling,
        int? tagId,
        DateTime? fromDate,
        DateTime? toDate,
        int? month,
        int? year,
        string? sortOrder)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var filter = new MemoryFilterViewModel
        {
            Search = CleanOptionalText(search),
            Feeling = CleanOptionalText(feeling),
            TagId = tagId,
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date,
            Month = month,
            Year = year,
            SortOrder = string.Equals(sortOrder, "oldest", StringComparison.OrdinalIgnoreCase) ? "oldest" : "newest"
        };

        var validationMessages = new List<string>();
        var baseQuery = _context.Memories
            .AsNoTracking()
            .Where(memory => memory.UserId == userId && !memory.IsDeleted);

        var totalMemoryCount = await baseQuery.CountAsync();
        var availableTags = await GetAvailableTagsAsync(userId);
        var availableYears = await baseQuery
            .Select(memory => memory.MemoryDate.Year)
            .Distinct()
            .OrderByDescending(memoryYear => memoryYear)
            .ToListAsync();

        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();

            query = query.Where(memory =>
                memory.Title.Contains(keyword) ||
                (memory.Story != null && memory.Story.Contains(keyword)) ||
                (memory.Location != null && memory.Location.Contains(keyword)) ||
                memory.MemoryTags.Any(memoryTag => memoryTag.Tag.Name.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Feeling))
        {
            query = query.Where(memory => memory.Feeling == filter.Feeling);
        }

        if (filter.TagId.HasValue)
        {
            query = query.Where(memory => memory.MemoryTags.Any(memoryTag => memoryTag.TagId == filter.TagId.Value));
        }

        query = ApplyDateFilters(query, filter, validationMessages);
        query = string.Equals(filter.SortOrder, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(memory => memory.MemoryDate).ThenBy(memory => memory.CreatedAt)
            : query.OrderByDescending(memory => memory.MemoryDate).ThenByDescending(memory => memory.CreatedAt);

        var memories = await query
            .Select(memory => new MemoryListItemViewModel
            {
                Id = memory.Id,
                Title = memory.Title,
                Story = memory.Story,
                Feeling = memory.Feeling,
                MemoryDate = memory.MemoryDate,
                Location = memory.Location,
                CoverImagePath = memory.Images
                    .OrderBy(image => image.UploadedAt)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault(),
                Tags = memory.MemoryTags
                    .OrderBy(memoryTag => memoryTag.Tag.Name)
                    .Select(memoryTag => memoryTag.Tag.Name)
                    .ToList()
            })
            .ToListAsync();

        return View(new MemoryIndexViewModel
        {
            Filter = filter,
            Memories = memories,
            AvailableTags = availableTags,
            AvailableYears = availableYears,
            ValidationMessages = validationMessages,
            TotalMemoryCount = totalMemoryCount
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var memory = await FindOwnedMemoryAsync(id, asTracking: false);

        if (memory is null)
        {
            return NotFound();
        }

        return View(ToDetailsViewModel(memory));
    }

    public IActionResult Create()
    {
        return View(new MemoryFormViewModel { MemoryDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemoryFormViewModel model)
    {
        ValidateImageUpload(model.NewImages, existingImageCount: 0);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;
        var memory = new Memory
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Story = CleanOptionalText(model.Story),
            Feeling = model.Feeling,
            MemoryDate = model.MemoryDate!.Value.Date,
            Location = CleanOptionalText(model.Location),
            CreatedAt = now,
            UpdatedAt = now
        };

        await ApplyTagsAsync(memory, model.TagsText);

        _context.Memories.Add(memory);
        await _context.SaveChangesAsync();

        await AddImagesAsync(memory, model.NewImages, userId);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = memory.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var memory = await FindOwnedMemoryAsync(id);

        if (memory is null)
        {
            return NotFound();
        }

        return View(new MemoryFormViewModel
        {
            Id = memory.Id,
            Title = memory.Title,
            Story = memory.Story,
            Feeling = memory.Feeling,
            MemoryDate = memory.MemoryDate,
            Location = memory.Location,
            ExistingImages = ToImageViewModels(memory.Images),
            TagsText = string.Join(", ", memory.MemoryTags
                .OrderBy(memoryTag => memoryTag.Tag.Name)
                .Select(memoryTag => memoryTag.Tag.Name))
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemoryFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        var memory = await FindOwnedMemoryAsync(id);

        if (memory is null)
        {
            return NotFound();
        }

        ValidateImageUpload(model.NewImages, memory.Images.Count);

        if (!ModelState.IsValid)
        {
            model.ExistingImages = ToImageViewModels(memory.Images);
            return View(model);
        }

        memory.Title = model.Title.Trim();
        memory.Story = CleanOptionalText(model.Story);
        memory.Feeling = model.Feeling;
        memory.MemoryDate = model.MemoryDate!.Value.Date;
        memory.Location = CleanOptionalText(model.Location);
        memory.UpdatedAt = DateTime.UtcNow;

        _context.MemoryTags.RemoveRange(memory.MemoryTags);
        memory.MemoryTags.Clear();
        await ApplyTagsAsync(memory, model.TagsText);
        await AddImagesAsync(memory, model.NewImages, memory.UserId);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = memory.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId, string? returnTo)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var image = await _context.MemoryImages
            .Include(memoryImage => memoryImage.Memory)
            .FirstOrDefaultAsync(memoryImage =>
                memoryImage.Id == imageId &&
                memoryImage.MemoryId == id &&
                memoryImage.Memory.UserId == userId &&
                !memoryImage.Memory.IsDeleted);

        if (image is null)
        {
            return NotFound();
        }

        var imagePath = image.ImagePath;

        _context.MemoryImages.Remove(image);
        await _context.SaveChangesAsync();

        _imageStorageService.DeleteImageFile(imagePath);

        if (string.Equals(returnTo, "details", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var memory = await FindOwnedMemoryAsync(id, asTracking: false);

        if (memory is null)
        {
            return NotFound();
        }

        return View(ToDetailsViewModel(memory));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var memory = await FindOwnedMemoryAsync(id);

        if (memory is null)
        {
            return NotFound();
        }

        memory.IsDeleted = true;
        memory.DeletedAt = DateTime.UtcNow;
        memory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<MemoryTagFilterOption>> GetAvailableTagsAsync(string userId)
    {
        return await _context.MemoryTags
            .AsNoTracking()
            .Where(memoryTag => memoryTag.Memory.UserId == userId && !memoryTag.Memory.IsDeleted)
            .Select(memoryTag => new MemoryTagFilterOption
            {
                Id = memoryTag.Tag.Id,
                Name = memoryTag.Tag.Name
            })
            .Distinct()
            .OrderBy(tag => tag.Name)
            .ToListAsync();
    }

    private static IQueryable<Memory> ApplyDateFilters(
        IQueryable<Memory> query,
        MemoryFilterViewModel filter,
        ICollection<string> validationMessages)
    {
        var hasDateRange = filter.FromDate.HasValue || filter.ToDate.HasValue;

        if (hasDateRange)
        {
            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value > filter.ToDate.Value)
            {
                validationMessages.Add("Tu ngay khong duoc lon hon den ngay. Vui long chon lai khoang thoi gian.");
                return query.Where(memory => false);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(memory => memory.MemoryDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(memory => memory.MemoryDate <= filter.ToDate.Value);
            }

            return query;
        }

        if (filter.Month.HasValue && !filter.Year.HasValue)
        {
            validationMessages.Add("Vui long chon nam khi loc theo thang. Bo loc thang dang duoc bo qua.");
            return query;
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(memory => memory.MemoryDate.Year == filter.Year.Value);

            if (filter.Month.HasValue)
            {
                query = query.Where(memory => memory.MemoryDate.Month == filter.Month.Value);
            }
        }

        return query;
    }

    private async Task<Memory?> FindOwnedMemoryAsync(int id, bool asTracking = true)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return null;
        }

        var query = _context.Memories
            .Include(memory => memory.Images)
            .Include(memory => memory.MemoryTags)
                .ThenInclude(memoryTag => memoryTag.Tag)
            .Where(memory => memory.Id == id && memory.UserId == userId && !memory.IsDeleted);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task ApplyTagsAsync(Memory memory, string? tagsText)
    {
        foreach (var tagName in ParseTags(tagsText))
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(existingTag => existingTag.Name == tagName);

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

    private static IReadOnlyList<string> ParseTags(string? tagsText)
    {
        if (string.IsNullOrWhiteSpace(tagsText))
        {
            return [];
        }

        return tagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void ValidateImageUpload(IReadOnlyCollection<IFormFile> newImages, int existingImageCount)
    {
        var realNewImages = newImages.Where(image => image.Length > 0).ToList();

        if (existingImageCount + realNewImages.Count > _imageStorageService.MaxImagesPerMemory)
        {
            ModelState.AddModelError(nameof(MemoryFormViewModel.NewImages), "Moi ky uc chi duoc luu toi da 10 anh.");
        }

        foreach (var error in _imageStorageService.ValidateImages(realNewImages))
        {
            ModelState.AddModelError(nameof(MemoryFormViewModel.NewImages), error);
        }
    }

    private async Task AddImagesAsync(Memory memory, IReadOnlyCollection<IFormFile> newImages, string userId)
    {
        foreach (var image in newImages.Where(image => image.Length > 0))
        {
            var savedImage = await _imageStorageService.SaveImageAsync(image, userId, memory.Id);

            memory.Images.Add(new MemoryImage
            {
                MemoryId = memory.Id,
                ImagePath = savedImage.ImagePath,
                OriginalFileName = savedImage.OriginalFileName,
                UploadedAt = DateTime.UtcNow
            });
        }
    }

    private static IReadOnlyList<MemoryImageViewModel> ToImageViewModels(IEnumerable<MemoryImage> images)
    {
        return images
            .OrderBy(image => image.UploadedAt)
            .Select(image => new MemoryImageViewModel
            {
                Id = image.Id,
                ImagePath = image.ImagePath,
                OriginalFileName = image.OriginalFileName,
                UploadedAt = image.UploadedAt,
                Caption = image.Caption
            })
            .ToList();
    }

    private static MemoryDetailsViewModel ToDetailsViewModel(Memory memory)
    {
        return new MemoryDetailsViewModel
        {
            Id = memory.Id,
            Title = memory.Title,
            Story = memory.Story,
            Feeling = memory.Feeling,
            MemoryDate = memory.MemoryDate,
            Location = memory.Location,
            Images = ToImageViewModels(memory.Images),
            Tags = memory.MemoryTags
                .OrderBy(memoryTag => memoryTag.Tag.Name)
                .Select(memoryTag => memoryTag.Tag.Name)
                .ToList(),
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt
        };
    }
}
