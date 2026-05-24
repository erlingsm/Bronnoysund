// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for local SQLite. Contains 5 tables for search history,
/// favorites, persistent cache, user settings, and configurable registry endpoints.
/// Detailed specification: /Plan/13-Persistens-og-cache.md
/// </summary>
public sealed class BronnoysundDbContext(DbContextOptions<BronnoysundDbContext> options) : DbContext(options)
{
    public DbSet<LookupHistoryEntry> LookupHistory => Set<LookupHistoryEntry>();
    public DbSet<FavoriteCompany> Favorites => Set<FavoriteCompany>();
    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<RegisterEndpoint> RegisterEndpoints => Set<RegisterEndpoint>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no native DateTimeOffset; store as int64 ticks so that Where/ExecuteDelete
        // can be compared server-side (without this, LRU eviction and history cleanup break).
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LookupHistoryEntry>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.SearchedAt);
            b.HasIndex(e => e.OrgNumber);
            b.HasIndex(e => e.IsFavorite);
        });

        modelBuilder.Entity<FavoriteCompany>().HasKey(e => e.OrgNumber);

        modelBuilder.Entity<CacheEntry>(b =>
        {
            b.HasKey(e => e.Key);
            b.HasIndex(e => e.LastAccessedAt);
            b.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<AppSetting>().HasKey(e => e.Key);
        modelBuilder.Entity<RegisterEndpoint>().HasKey(e => e.Name);

        base.OnModelCreating(modelBuilder);
    }
}
