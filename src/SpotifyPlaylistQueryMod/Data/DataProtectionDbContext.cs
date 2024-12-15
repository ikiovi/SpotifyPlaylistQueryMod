using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SpotifyPlaylistQueryMod.Data;

public sealed class DataProtectionDbContext : DbContext, IDataProtectionKeyContext
{
    public const string ConnectionString = "DPAPIContext";

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DataProtectionDbContext(DbContextOptions<DataProtectionDbContext> options) : base(options) { }
}
