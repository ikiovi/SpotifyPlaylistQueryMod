using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyPlaylistQueryMod.Data;

namespace SpotifyPlaylistQueryMod;

public static partial class DependencyInjection
{
    public static IServiceCollection SetupStorageServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddCache(config);
        services.AddDPDatabase(config);
        services.ConfigureDataProtection(config);
        services.AddApplicationDatabase(config);
        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = config.GetConnectionString("RedisCache");
        });
        return services;
    }

    public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString(ApplicationDbContext.ConnectionString)));

        return services;
    }

    public static IServiceCollection AddDPDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<DataProtectionDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString(DataProtectionDbContext.ConnectionString)));

        return services;
    }

    public static IServiceCollection ConfigureDataProtection(this IServiceCollection services, IConfiguration config)
    {
        services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionDbContext>()
            .SetDefaultKeyLifetime(TimeSpan.FromDays(7)); //TODO: Configuration

        return services;
    }

    public static Task InitializeApplicationDatabaseAsync(this IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Database.MigrateAsync();
    }

    public static Task InitializeDPDatabaseAsync(this IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<DataProtectionDbContext>();
        return context.Database.EnsureCreatedAsync();
    }
}
