using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Data;

public sealed class ApplicationDbContext : DbContext
{
    public const string ConnectionString = "DefaultConnection";
    public DbSet<User> Users { get; set; }
    public DbSet<SourcePlaylist> SourcePlaylists { get; set; }
    public DbSet<PlaylistQueryInfo> QueriesInfo { get; set; }
    public DbSet<PlaylistQueryState> QueriesState { get; set; }
    public DbSet<DestinationPlaylist> DestinationPlaylists { get; set; }
    public DbSet<Track> Tracks { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}