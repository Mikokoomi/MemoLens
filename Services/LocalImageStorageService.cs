using Microsoft.AspNetCore.Http;

namespace MemoLens.Services;

public class LocalImageStorageService : IImageStorageService
{
    private const string PrivateRoot = "App_Data";
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
                errors.Add($"File \"{file.FileName}\" không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WEBP.");
                continue;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errors.Add($"File \"{file.FileName}\" vượt quá giới hạn 5MB.");
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

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await file.CopyToAsync(stream);
        }
        catch
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }

        return new ImageUploadResult
        {
            ImagePath = Path.Combine(UploadRoot, MemoryFolder, userFolder, memoryId.ToString(), safeFileName)
                .Replace('\\', '/'),
            OriginalFileName = Path.GetFileName(file.FileName)
        };
    }

    public void DeleteImageFile(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var fullPath = ResolveImagePath(imagePath) ?? ResolveLegacyPublicImagePath(imagePath);

        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public string? ResolveImagePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var trimmedPath = imagePath.TrimStart('/', '\\');
        var fullPath = Path.GetFullPath(Path.Combine(GetPrivateRootPath(), trimmedPath));
        var privateRootPath = GetPrivateRootPath();

        if (!IsPathInsideRoot(fullPath, privateRootPath))
        {
            return null;
        }

        return fullPath;
    }

    public string GetContentType(string imagePath)
    {
        return Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private string GetUploadRootPath()
    {
        return Path.GetFullPath(Path.Combine(GetPrivateRootPath(), UploadRoot));
    }

    private string GetPrivateRootPath()
    {
        return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, PrivateRoot));
    }

    private string? ResolveLegacyPublicImagePath(string imagePath)
    {
        var trimmedPath = imagePath.TrimStart('/', '\\');
        var fullPath = Path.GetFullPath(Path.Combine(GetWebRootPath(), trimmedPath));
        var legacyUploadRootPath = Path.GetFullPath(Path.Combine(GetWebRootPath(), UploadRoot));

        if (!IsPathInsideRoot(fullPath, legacyUploadRootPath))
        {
            return null;
        }

        return fullPath;
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

    private static bool IsPathInsideRoot(string fullPath, string rootPath)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(rootPath) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(fullPath);

        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
