// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Persistence.Cache;
using Bronnoysund.Infrastructure.Persistence.Configuration;
using Bronnoysund.Infrastructure.Persistence.Maintenance;
using Bronnoysund.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers BronnoysundDbContext + SQLite + repositories + persistent cache (L2) +
    /// maintenance jobs. The caller of this extension method must register IDatabasePathProvider
    /// up front (DefaultDatabasePathProvider in WebApi/Blazor, MauiDatabasePathProvider in MAUI).
    ///
    /// The schema is created via EnsureCreatedAsync at startup. To be replaced with proper code-first
    /// migrations once we make the first schema change after users have data on disk.
    /// </summary>
    public static IServiceCollection AddBronnoysundPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName));

        services.AddDbContext<BronnoysundDbContext>((sp, opts) =>
        {
            var pathProvider = sp.GetRequiredService<IDatabasePathProvider>();
            opts.UseSqlite($"Data Source={pathProvider.GetDatabaseFilePath()}");
        });

        services.AddScoped<ILookupHistoryRepository, LookupHistoryRepository>();
        services.AddScoped<IFavoritesRepository, FavoritesRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IRegisterEndpointsRepository, RegisterEndpointsRepository>();
        services.AddScoped<ISettingsBackup, SettingsBackup>();

        services.AddSingleton<IDistributedCache, SqliteCacheStore>();
        services.AddSingleton<IHybridCacheSerializerFactory, GzipHybridCacheSerializerFactory>();

        services.AddScoped<HistoryCleanupService>();
        services.AddScoped<CacheEvictionService>();
        services.AddHostedService<PersistenceMaintenanceHostedService>();

        return services;
    }

    /// <summary>
    /// Creates the SQLite file + schema if the file does not exist. Call from Program.cs or
    /// MauiProgram.cs after Build(): <c>await app.Services.InitializeBronnoysundPersistenceAsync()</c>.
    /// </summary>
    public static async Task InitializeBronnoysundPersistenceAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
    }
}
