using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Data.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id)
        .IsUnicode(false)
        .ValueGeneratedNever();

        b.Property(u => u.Privileges)
        .HasConversion<int>();

        b.Property(u => u.RefreshToken)
        .IsRequired();

        b.Property(u => u.NextRefresh)
        .IsRequired();

        b.HasMany<PlaylistQueryInfo>()
        .WithOne()
        .HasForeignKey(q => q.UserId);

        b.HasIndex(u => u.Privileges)
        .HasDatabaseName("ix_unique_superadmin")
        .IsUnique()
        .HasFilter($"{nameof(User.Privileges)} = {(int)UserPrivileges.SuperAdmin}");
    }
}
