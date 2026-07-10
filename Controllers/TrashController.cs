using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Trash;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

[Authorize]
public class TrashController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrashController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        var deletedMemories = await _context.Memories
            .AsNoTracking()
            .Where(memory => memory.UserId == userId && memory.IsDeleted)
            .OrderByDescending(memory => memory.DeletedAt)
            .ThenByDescending(memory => memory.UpdatedAt)
            .Select(memory => new DeletedMemoryViewModel
            {
                Id = memory.Id,
                Title = memory.Title,
                Story = memory.Story,
                Feeling = memory.Feeling,
                MemoryDate = memory.MemoryDate,
                Location = memory.Location,
                DeletedAt = memory.DeletedAt
            })
            .ToListAsync();

        var deletedAlbums = await _context.Albums
            .AsNoTracking()
            .Where(album => album.UserId == userId && album.IsDeleted)
            .OrderByDescending(album => album.DeletedAt)
            .ThenByDescending(album => album.UpdatedAt)
            .Select(album => new DeletedAlbumViewModel
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                DeletedAt = album.DeletedAt
            })
            .ToListAsync();

        return View(new TrashIndexViewModel
        {
            DeletedMemories = deletedMemories,
            DeletedAlbums = deletedAlbums
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreMemory(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var memory = await _context.Memories
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId && item.IsDeleted);

        if (memory is null)
        {
            TempData["TrashMessage"] = "Không tìm thấy mục cần khôi phục.";
            return NotFound();
        }

        memory.IsDeleted = false;
        memory.DeletedAt = null;
        memory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["TrashMessage"] = "Đã khôi phục kỷ niệm.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreAlbum(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var album = await _context.Albums
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId && item.IsDeleted);

        if (album is null)
        {
            TempData["TrashMessage"] = "Không tìm thấy mục cần khôi phục.";
            return NotFound();
        }

        album.IsDeleted = false;
        album.DeletedAt = null;
        album.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["TrashMessage"] = "Đã khôi phục bộ sưu tập.";

        return RedirectToAction(nameof(Index));
    }
}
