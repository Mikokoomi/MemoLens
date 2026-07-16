using MemoLens.Data;
using MemoLens.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Services;

public sealed class CoverResolutionService : ICoverResolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorageService;

    public CoverResolutionService(
        ApplicationDbContext context,
        IImageStorageService imageStorageService)
    {
        _context = context;
        _imageStorageService = imageStorageService;
    }

    public int? ResolveEffectiveMemoryCoverImageId(Memory memory)
    {
        var manual = GetValidMemoryCover(memory);
        if (manual is not null)
        {
            return manual.Id;
        }

        return memory.Images
            .OrderBy(image => image.UploadedAt)
            .ThenBy(image => image.Id)
            .FirstOrDefault(IsAccessibleImage)
            ?.Id;
    }

    public int? ResolveEffectiveAlbumCoverImageId(Album album)
    {
        var manual = GetValidAlbumCover(album);
        if (manual is not null)
        {
            return manual.Id;
        }

        return album.AlbumMemories
            .Where(membership => !membership.Memory.IsDeleted)
            .OrderByDescending(membership => membership.AddedAt)
            .ThenByDescending(membership => membership.MemoryId)
            .Select(membership => ResolveEffectiveMemoryCoverImageId(membership.Memory))
            .FirstOrDefault(imageId => imageId.HasValue);
    }

    public bool ClearInvalidMemoryManualCover(Memory memory)
    {
        if (!memory.CoverImageId.HasValue || GetValidMemoryCover(memory) is not null)
        {
            return false;
        }

        memory.CoverImageId = null;
        return true;
    }

    public bool ClearInvalidAlbumManualCover(Album album)
    {
        if (!album.CoverImageId.HasValue || GetValidAlbumCover(album) is not null)
        {
            return false;
        }

        album.CoverImageId = null;
        return true;
    }

    public async Task ClearCoverReferencesForImageAsync(int imageId)
    {
        var memories = await _context.Memories
            .Where(memory => memory.CoverImageId == imageId)
            .ToListAsync();
        var albums = await _context.Albums
            .Where(album => album.CoverImageId == imageId)
            .ToListAsync();

        foreach (var memory in memories)
        {
            memory.CoverImageId = null;
        }

        foreach (var album in albums)
        {
            album.CoverImageId = null;
        }
    }

    public async Task ClearAlbumCoverReferencesForMemoryAsync(int memoryId)
    {
        var imageIds = await _context.MemoryImages
            .Where(image => image.MemoryId == memoryId)
            .Select(image => image.Id)
            .ToListAsync();

        if (imageIds.Count == 0)
        {
            return;
        }

        var albums = await _context.Albums
            .Where(album => album.CoverImageId.HasValue && imageIds.Contains(album.CoverImageId.Value))
            .ToListAsync();

        foreach (var album in albums)
        {
            album.CoverImageId = null;
        }
    }

    public async Task ClearAlbumCoverReferenceForRemovedMemoryAsync(int albumId, int memoryId)
    {
        var coverImageId = await _context.Albums
            .Where(album => album.Id == albumId)
            .Select(album => album.CoverImageId)
            .FirstOrDefaultAsync();
        if (!coverImageId.HasValue)
        {
            return;
        }

        var isSourceImage = await _context.MemoryImages
            .AnyAsync(image => image.Id == coverImageId.Value && image.MemoryId == memoryId);
        if (!isSourceImage)
        {
            return;
        }

        var album = await _context.Albums.FirstAsync(item => item.Id == albumId);
        album.CoverImageId = null;
    }

    private MemoryImage? GetValidMemoryCover(Memory memory) => memory.CoverImageId is int coverImageId
        ? memory.Images.FirstOrDefault(image => image.Id == coverImageId && IsAccessibleImage(image))
        : null;

    private MemoryImage? GetValidAlbumCover(Album album)
    {
        if (!album.CoverImageId.HasValue)
        {
            return null;
        }

        return album.AlbumMemories
            .Where(membership => !membership.Memory.IsDeleted)
            .SelectMany(membership => membership.Memory.Images)
            .FirstOrDefault(image => image.Id == album.CoverImageId.Value && IsAccessibleImage(image));
    }

    private bool IsAccessibleImage(MemoryImage image)
    {
        var filePath = _imageStorageService.ResolveImagePath(image.ImagePath);
        return filePath is not null && File.Exists(filePath);
    }
}
