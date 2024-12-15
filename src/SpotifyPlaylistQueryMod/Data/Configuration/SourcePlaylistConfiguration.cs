using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class SourcePlaylistConfiguration : IEntityTypeConfiguration<SourcePlaylist>
{
    public void Configure(EntityTypeBuilder<SourcePlaylist> b)
    {
        b.ToTable("source_playlists");
        b.HasKey(p => p.Id);

        b.Property(p => p.Id)
        .IsUnicode(false)
        .ValueGeneratedNever();

        b.Property(p => p.OwnerId)
        .IsRequired()
        .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);

        b.Property(p => p.SnapshotId)
        .IsUnicode(false)
        .IsRequired(true);

        b.HasMany<Track>()
        .WithOne()
        .HasForeignKey(j => j.SourcePlaylistId);
    }
}
