// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Persistence.Cache;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using Bronnoysund.Lookup.Infrastructure.Persistence.Maintenance;
using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registrerer BronnoysundDbContext + SQLite + repositories + persistent cache (L2) +
    /// vedlikeholds-jobber. Brukeren av extension-metoden må registrere IDatabasePathProvider
    /// på forhånd (DefaultDatabasePathProvider i WebApi/Blazor, MauiDatabasePathProvider i MAUI).
    ///
    /// Skjema opprettes via EnsureCreatedAsync ved oppstart. Skal byttes til proper code-first
    /// migrations når vi gjør første schema-endring etter at brukere har data på disk.
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
    /// Oppretter SQLite-fil + skjema hvis fila ikke finnes. Kall fra Program.cs eller
    /// MauiProgram.cs etter Build(): <c>await app.Services.InitializeBronnoysundPersistenceAsync()</c>.
    /// </summary>
    public static async Task InitializeBronnoysundPersistenceAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
    }
}
