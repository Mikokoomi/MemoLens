using Microsoft.AspNetCore.Http;

namespace MemoLens.Services;

public class LocalImageStorageService : IImageStorageService
{
    private const string UploadRoot = "uploads";
    private const string MemoryFolder = "memories";
    private readonly IWebHostEnvironment _environment;

    public LocalImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public int MaxImagesPerMemory => 10;

    public long MaxFileSizeBytes => 5 * 1024 * 1024;

    public IReadOnlySet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public IReadOnlyList<string> ValidateImages(IEnumerable<IFormFile> files)
    {
        var errors = new List<string>();

        foreach (var file in files.Where(file => file.Length > 0))
        {
            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                errors.Add($"File \"{file.FileName}\" khong duoc ho tro. Chi chap nhan JPG, PNG, WEBP.");
                continue;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errors.Add($"File \"{file.FileName}\" vuot qua gioi han 5MB.");
            }
        }

        return errors;
    }

    public async Task<ImageUploadResult> SaveImageAsync(IFormFile file, string userId, int memoryId)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var userFolder = GetSafeFolderName(userId);
        var uploadFolder = Path.Combine(GetUploadRootPath(), MemoryFolder, userFolder, memoryId.ToString());

        Directory.CreateDirectory(uploadFolder);

        var filePath = Path.Combine(uploadFolder, safeFileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        return new ImageUploadResult
        {
            ImagePath = $"/{UploadRoot}/{MemoryFolder}/{userFolder}/{memoryId}/{safeFileName}",
            OriginalFileName = Path.GetFileName(file.FileName)
        };
    }

    public void DeleteImageFile(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var uploadsRoot = GetUploadRootPath();
        var trimmedPath = imagePath.TrimStart('/', '\\');
        var fullPath = Path.GetFullPath(Path.Combine(GetWebRootPath(), trimmedPath));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string GetUploadRootPath()
    {
        return Path.GetFullPath(Path.Combine(GetWebRootPath(), UploadRoot));
    }

    private string GetWebRootPath()
    {
        return _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
    }

    private static string GetSafeFolderName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeChars = value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray();

        return new string(safeChars);
    }
}
