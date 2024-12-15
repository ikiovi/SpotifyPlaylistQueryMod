using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class PlaylistQueryInfoConfiguration : IEntityTypeConfiguration<PlaylistQueryInfo>
{
    public void Configure(EntityTypeBuilder<PlaylistQueryInfo> b)
    {
        b.ToTable("queries");
        b.HasKey(q => q.Id);

        b.Property(q => q.Id)
        .ValueGeneratedOnAdd();

        b.Property(q => q.SourceId)
        .IsRequired();

        b.Property(q => q.UserId)
        .IsRequired();

        b.Property(q => q.Query)
        .IsRequired();

        b.Property(q => q.Version)
        .IsConcurrencyToken();

        b.HasOne<SourcePlaylist>()
        .WithMany()
        .HasForeignKey(q => q.SourceId);

        b.HasOne<DestinationPlaylist>()
        .WithOne(p => p.ActiveQuery)
        .HasForeignKey<PlaylistQueryInfo>(q => q.TargetId);
    }
}
