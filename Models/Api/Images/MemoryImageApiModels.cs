using MemoLens.Models.Api.Memories;
using Microsoft.AspNetCore.Http;

namespace MemoLens.Models.Api.Images;

public sealed class UploadMemoryImagesRequest
{
    public List<IFormFile> Files { get; set; } = [];
}

public sealed class UploadMemoryImagesResponse
{
    public IReadOnlyList<MemoryImageResponse> Images { get; init; } = [];

    public int TotalImageCount { get; init; }

    public int RemainingSlots { get; init; }
}

public static class MemoryImageApiRoutes
{
    public static string Content(int imageId) => $"/api/v1/images/{imageId}/content";
}
