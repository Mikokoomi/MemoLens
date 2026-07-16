using MemoLens.Models;

namespace MemoLens.Services;

public interface ICoverResolutionService
{
    int? ResolveEffectiveMemoryCoverImageId(Memory memory);

    int? ResolveEffectiveAlbumCoverImageId(Album album);

    bool ClearInvalidMemoryManualCover(Memory memory);

    bool ClearInvalidAlbumManualCover(Album album);

    Task ClearCoverReferencesForImageAsync(int imageId);

    Task ClearAlbumCoverReferencesForMemoryAsync(int memoryId);

    Task ClearAlbumCoverReferenceForRemovedMemoryAsync(int albumId, int memoryId);
}
