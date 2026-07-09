using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

[Authorize]
public class ImagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ImagesController(
        ApplicationDbContext context,
        IImageStorageService imageStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _imageStorageService = imageStorageService;
        _userManager = userManager;
    }

    public async Task<IActionResult> MemoryImage(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId is null)
        {
            return NotFound();
        }

        var image = await _context.MemoryImages
            .AsNoTracking()
            .Include(memoryImage => memoryImage.Memory)
            .FirstOrDefaultAsync(memoryImage =>
                memoryImage.Id == id &&
                memoryImage.Memory.UserId == userId &&
                !memoryImage.Memory.IsDeleted);

        if (image is null)
        {
            return NotFound();
        }

        var filePath = _imageStorageService.ResolveImagePath(image.ImagePath);

        if (filePath is null || !System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = _imageStorageService.GetContentType(image.ImagePath);

        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }
}
