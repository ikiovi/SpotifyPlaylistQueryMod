using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> b)
    {
        b.HasKey(j => j.Id);
        b.Property(j => j.Id)
        .ValueGeneratedOnAdd();

        b.Property(j => j.SourcePlaylistId)
        .IsRequired();

        b.Property(j => j.AddedAt)
        .IsRequired()
        .ValueGeneratedNever();
    }
}