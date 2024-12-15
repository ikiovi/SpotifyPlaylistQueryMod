using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class DestinationPlaylistConfiguration : IEntityTypeConfiguration<DestinationPlaylist>
{
    public void Configure(EntityTypeBuilder<DestinationPlaylist> b)
    {
        b.ToTable("target_playlists");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id)
        .IsUnicode(false)
        .ValueGeneratedNever();

        b.Property(p => p.OwnerId)
        .IsRequired();
    }
}
