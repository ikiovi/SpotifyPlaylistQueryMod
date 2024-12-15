using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class PlaylistQueryStateConfiguration : IEntityTypeConfiguration<PlaylistQueryState>
{
    public void Configure(EntityTypeBuilder<PlaylistQueryState> b)
    {
        b.ToTable("queries_state");
        b.HasKey(q => q.Id);

        b.Property(q => q.Id)
        .ValueGeneratedNever();

        b.HasOne(q => q.Info)
        .WithOne()
        .HasForeignKey<PlaylistQueryState>();

        b.Navigation(q => q.Info).AutoInclude();
    }
}
