// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for lokal SQLite. Inneholder 5 tabeller for søkehistorikk,
/// favoritter, persistent cache, brukerinnstillinger og konfigurerbare register-endepunkter.
/// Detaljert spesifikasjon: /Plan/13-Persistens-og-cache.md
/// </summary>
public sealed class BronnoysundDbContext(DbContextOptions<BronnoysundDbContext> options) : DbContext(options)
{
    public DbSet<LookupHistoryEntry> LookupHistory => Set<LookupHistoryEntry>();
    public DbSet<FavoriteCompany> Favorites => Set<FavoriteCompany>();
    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<RegisterEndpoint> RegisterEndpoints => Set<RegisterEndpoint>();

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
