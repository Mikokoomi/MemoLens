using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Albums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

[Authorize]
public class AlbumsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AlbumsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var albums = await _context.Albums
            .AsNoTracking()
            .Include(album => album.AlbumMemories)
                .ThenInclude(albumMemory => albumMemory.Memory)
                    .ThenInclude(memory => memory.Images)
            .Where(album => album.UserId == userId && !album.IsDeleted)
            .OrderByDescending(album => album.UpdatedAt)
            .ThenByDescending(album => album.CreatedAt)
            .ToListAsync();

        return View(albums.Select(ToListItemViewModel).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var album = await FindOwnedAlbumAsync(id, asTracking: false);

        if (album is null)
        {
            return NotFound();
        }

        return View(ToDetailsViewModel(album));
    }

    public IActionResult Create()
    {
        return View(new AlbumFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AlbumFormViewModel model)
    {
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
        var album = new Album
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Description = CleanOptionalText(model.Description),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = album.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var album = await FindOwnedAlbumAsync(id);

        if (album is null)
        {
            return NotFound();
        }

        return View(new AlbumFormViewModel
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AlbumFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        var album = await FindOwnedAlbumAsync(id);

        if (album is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        album.Title = model.Title.Trim();
        album.Description = CleanOptionalText(model.Description);
        album.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = album.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var album = await FindOwnedAlbumAsync(id, asTracking: false);

        if (album is null)
        {
            return NotFound();
        }

        return View(ToDetailsViewModel(album));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var album = await FindOwnedAlbumAsync(id);

        if (album is null)
        {
            return NotFound();
        }

        album.IsDeleted = true;
        album.DeletedAt = DateTime.UtcNow;
        album.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AddMemories(int id)
    {
        var album = await FindOwnedAlbumAsync(id, asTracking: false);

        if (album is null)
        {
            return NotFound();
        }

        return View(await BuildAddMemoriesViewModelAsync(album));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMemories(int id, AlbumAddMemoriesViewModel model)
    {
        if (model.AlbumId != id)
        {
            return NotFound();
        }

        var album = await FindOwnedAlbumAsync(id);

        if (album is null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var selectedIds = model.SelectedMemoryIds.Distinct().ToList();

        if (!selectedIds.Any())
        {
            ModelState.AddModelError(nameof(model.SelectedMemoryIds), "Vui long chon it nhat mot ky niem.");
            return View(await BuildAddMemoriesViewModelAsync(album, selectedIds));
        }

        var existingMemoryIds = album.AlbumMemories
            .Select(albumMemory => albumMemory.MemoryId)
            .ToHashSet();

        var ownedMemories = await _context.Memories
            .Where(memory =>
                memory.UserId == userId &&
                !memory.IsDeleted &&
                selectedIds.Contains(memory.Id) &&
                !existingMemoryIds.Contains(memory.Id))
            .Select(memory => memory.Id)
            .ToListAsync();

        if (!ownedMemories.Any())
        {
            ModelState.AddModelError(nameof(model.SelectedMemoryIds), "Khong co ky niem hop le de them vao bo suu tap nay.");
            return View(await BuildAddMemoriesViewModelAsync(album, selectedIds));
        }

        var now = DateTime.UtcNow;

        foreach (var memoryId in ownedMemories)
        {
            album.AlbumMemories.Add(new AlbumMemory
            {
                AlbumId = album.Id,
                MemoryId = memoryId,
                AddedAt = now
            });
        }

        album.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = album.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMemory(int id, int memoryId)
    {
        var album = await FindOwnedAlbumAsync(id);

        if (album is null)
        {
            return NotFound();
        }

        var albumMemory = album.AlbumMemories.FirstOrDefault(item => item.MemoryId == memoryId);

        if (albumMemory is null || albumMemory.Memory.UserId != album.UserId)
        {
            return NotFound();
        }

        _context.AlbumMemories.Remove(albumMemory);
        album.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = album.Id });
    }

    private async Task<Album?> FindOwnedAlbumAsync(int id, bool asTracking = true)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return null;
        }

        var query = _context.Albums
            .Include(album => album.AlbumMemories)
                .ThenInclude(albumMemory => albumMemory.Memory)
                    .ThenInclude(memory => memory.Images)
            .Where(album => album.Id == id && album.UserId == userId && !album.IsDeleted);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<AlbumAddMemoriesViewModel> BuildAddMemoriesViewModelAsync(Album album, List<int>? selectedIds = null)
    {
        var existingMemoryIds = album.AlbumMemories
            .Select(albumMemory => albumMemory.MemoryId)
            .ToHashSet();

        var memories = await _context.Memories
            .AsNoTracking()
            .Include(memory => memory.Images)
            .Where(memory => memory.UserId == album.UserId && !memory.IsDeleted && !existingMemoryIds.Contains(memory.Id))
            .OrderByDescending(memory => memory.MemoryDate)
            .ThenByDescending(memory => memory.CreatedAt)
            .ToListAsync();

        return new AlbumAddMemoriesViewModel
        {
            AlbumId = album.Id,
            AlbumTitle = album.Title,
            SelectedMemoryIds = selectedIds ?? [],
            AvailableMemories = memories.Select(memory => ToMemoryItemViewModel(memory)).ToList()
        };
    }

    private static AlbumListItemViewModel ToListItemViewModel(Album album)
    {
        var visibleMemories = GetVisibleAlbumMemories(album).ToList();

        return new AlbumListItemViewModel
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description,
            CoverImagePath = GetAutomaticCoverImagePath(visibleMemories),
            MemoryCount = visibleMemories.Count,
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt
        };
    }

    private static AlbumDetailsViewModel ToDetailsViewModel(Album album)
    {
        var visibleMemories = GetVisibleAlbumMemories(album).ToList();

        return new AlbumDetailsViewModel
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description,
            CoverImagePath = GetAutomaticCoverImagePath(visibleMemories),
            CreatedAt = album.CreatedAt,
            UpdatedAt = album.UpdatedAt,
            Memories = visibleMemories
                .Select(albumMemory => ToMemoryItemViewModel(albumMemory.Memory, albumMemory.AddedAt))
                .ToList()
        };
    }

    private static IEnumerable<AlbumMemory> GetVisibleAlbumMemories(Album album)
    {
        return album.AlbumMemories
            .Where(albumMemory => !albumMemory.Memory.IsDeleted)
            .OrderBy(albumMemory => albumMemory.AddedAt)
            .ThenBy(albumMemory => albumMemory.Memory.MemoryDate);
    }

    private static string? GetAutomaticCoverImagePath(IEnumerable<AlbumMemory> albumMemories)
    {
        return albumMemories
            .SelectMany(albumMemory => albumMemory.Memory.Images.OrderBy(image => image.UploadedAt))
            .Select(image => image.ImagePath)
            .FirstOrDefault();
    }

    private static AlbumMemoryItemViewModel ToMemoryItemViewModel(Memory memory, DateTime? addedAt = null)
    {
        return new AlbumMemoryItemViewModel
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
            AddedAt = addedAt
        };
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
