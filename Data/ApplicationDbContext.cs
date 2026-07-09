using MemoLens.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Memory> Memories => Set<Memory>();

    public DbSet<MemoryImage> MemoryImages => Set<MemoryImage>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<MemoryTag> MemoryTags => Set<MemoryTag>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<AlbumMemory> AlbumMemories => Set<AlbumMemory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(100);

            entity.Property(user => user.CreatedAt)
                .IsRequired();
        });

        builder.Entity<Memory>(entity =>
        {
            entity.Property(memory => memory.Title)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(memory => memory.Story)
                .HasMaxLength(4000);

            entity.Property(memory => memory.Feeling)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(memory => memory.MemoryDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(memory => memory.Location)
                .HasMaxLength(200);

            entity.Property(memory => memory.CreatedAt)
                .IsRequired();

            entity.Property(memory => memory.UpdatedAt)
                .IsRequired();

            entity.Property(memory => memory.IsDeleted)
                .IsRequired();

            entity.HasIndex(memory => memory.UserId);
            entity.HasIndex(memory => memory.MemoryDate);
            entity.HasIndex(memory => memory.Feeling);

            entity.HasOne(memory => memory.User)
                .WithMany(user => user.Memories)
                .HasForeignKey(memory => memory.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MemoryImage>(entity =>
        {
            entity.Property(image => image.ImagePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(image => image.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(image => image.Caption)
                .HasMaxLength(255);

            entity.Property(image => image.UploadedAt)
                .IsRequired();

            entity.HasOne(image => image.Memory)
                .WithMany(memory => memory.Images)
                .HasForeignKey(image => image.MemoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.Property(tag => tag.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(tag => tag.Name)
                .IsUnique();
        });

        builder.Entity<MemoryTag>(entity =>
        {
            entity.HasKey(memoryTag => new { memoryTag.MemoryId, memoryTag.TagId });

            entity.HasOne(memoryTag => memoryTag.Memory)
                .WithMany(memory => memory.MemoryTags)
                .HasForeignKey(memoryTag => memoryTag.MemoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(memoryTag => memoryTag.Tag)
                .WithMany(tag => tag.MemoryTags)
                .HasForeignKey(memoryTag => memoryTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Album>(entity =>
        {
            entity.Property(album => album.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(album => album.Description)
                .HasMaxLength(500);

            entity.Property(album => album.CoverImagePath)
                .HasMaxLength(500);

            entity.Property(album => album.CreatedAt)
                .IsRequired();

            entity.Property(album => album.UpdatedAt)
                .IsRequired();

            entity.Property(album => album.IsDeleted)
                .IsRequired();

            entity.HasIndex(album => album.UserId);
            entity.HasIndex(album => album.UpdatedAt);

            entity.HasOne(album => album.User)
                .WithMany(user => user.Albums)
                .HasForeignKey(album => album.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AlbumMemory>(entity =>
        {
            entity.HasKey(albumMemory => new { albumMemory.AlbumId, albumMemory.MemoryId });

            entity.Property(albumMemory => albumMemory.AddedAt)
                .IsRequired();

            entity.HasOne(albumMemory => albumMemory.Album)
                .WithMany(album => album.AlbumMemories)
                .HasForeignKey(albumMemory => albumMemory.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(albumMemory => albumMemory.Memory)
                .WithMany(memory => memory.AlbumMemories)
                .HasForeignKey(albumMemory => albumMemory.MemoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
