using Microsoft.AspNetCore.Http;

namespace MemoLens.Services;

public interface IImageStorageService
{
    int MaxImagesPerMemory { get; }

    long MaxFileSizeBytes { get; }

    IReadOnlySet<string> AllowedExtensions { get; }

    IReadOnlyList<string> ValidateImages(IEnumerable<IFormFile> files);

    Task<ImageUploadResult> SaveImageAsync(IFormFile file, string userId, int memoryId);

    void DeleteImageFile(string imagePath);

    string? ResolveImagePath(string imagePath);

    string GetContentType(string imagePath);
}
