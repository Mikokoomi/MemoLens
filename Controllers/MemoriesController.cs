using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Memories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

[Authorize]
public class MemoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MemoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        var memories = await _context.Memories
            .AsNoTracking()
            .Include(memory => memory.MemoryTags)
                .ThenInclude(memoryTag => memoryTag.Tag)
            .Where(memory => memory.UserId == userId && !memory.IsDeleted)
            .OrderByDescending(memory => memory.MemoryDate)
            .ThenByDescending(memory => memory.CreatedAt)
            .Select(memory => new MemoryListItemViewModel
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
                    .ToList()
            })
            .ToListAsync();

        return View(memories);
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var memory = await FindOwnedMemoryAsync(id);

        if (memory is null)
        {
            return NotFound();
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
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = memory.Id });
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

    private async Task<Memory?> FindOwnedMemoryAsync(int id, bool asTracking = true)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return null;
        }

        var query = _context.Memories
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
            Tags = memory.MemoryTags
                .OrderBy(memoryTag => memoryTag.Tag.Name)
                .Select(memoryTag => memoryTag.Tag.Name)
                .ToList(),
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt
        };
    }
}
