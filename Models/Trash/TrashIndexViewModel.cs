namespace MemoLens.Models.Trash;

public class TrashIndexViewModel
{
    public IReadOnlyList<DeletedMemoryViewModel> DeletedMemories { get; set; } = [];

    public IReadOnlyList<DeletedAlbumViewModel> DeletedAlbums { get; set; } = [];

    public bool IsEmpty => !DeletedMemories.Any() && !DeletedAlbums.Any();
}
