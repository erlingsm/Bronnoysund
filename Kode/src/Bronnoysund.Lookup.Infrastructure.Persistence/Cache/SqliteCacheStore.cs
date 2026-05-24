// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Cache;

/// <summary>
/// IDistributedCache backed by SQLite. Used as L2 behind HybridCache (L1 = memory).
/// Values are already GZip-compressed in the serializer layer (GzipHybridCacheSerializer),
/// so we store the bytes as-is. LastAccessedAt is updated on every Get for LRU.
/// </summary>
internal sealed class SqliteCacheStore(
    IServiceScopeFactory scopes,
    IOptionsMonitor<PersistenceOptions> opts) : IDistributedCache
{
    public byte[]? Get(string key) => GetAsync(key, default).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();

        var entry = await db.CacheEntries.FindAsync([key], token);
        if (entry is null) return null;

        var now = DateTimeOffset.UtcNow;
        if (entry.ExpiresAt < now)
        {
            db.CacheEntries.Remove(entry);
            await db.SaveChangesAsync(token);
            return null;
        }

        entry.LastAccessedAt = now;
        await db.SaveChangesAsync(token);
        return entry.Value;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => SetAsync(key, value, options, default).GetAwaiter().GetResult();

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();

        var now = DateTimeOffset.UtcNow;
        var expires = ResolveExpiration(options, now);

        var existing = await db.CacheEntries.FindAsync([key], token);
        if (existing is null)
        {
            db.CacheEntries.Add(new CacheEntry
            {
                Key = key,
                Value = value,
                SizeBytes = value.Length,
                StoredAt = now,
                ExpiresAt = expires,
                LastAccessedAt = now,
            });
        }
        else
        {
            db.CacheEntries.Remove(existing);
            db.CacheEntries.Add(new CacheEntry
            {
                Key = key,
                Value = value,
                SizeBytes = value.Length,
                StoredAt = now,
                ExpiresAt = expires,
                LastAccessedAt = now,
            });
        }

        await db.SaveChangesAsync(token);
    }

    public void Refresh(string key) => RefreshAsync(key, default).GetAwaiter().GetResult();

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();

        await db.CacheEntries
            .Where(c => c.Key == key)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastAccessedAt, DateTimeOffset.UtcNow), token);
    }

    public void Remove(string key) => RemoveAsync(key, default).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();

        await db.CacheEntries.Where(c => c.Key == key).ExecuteDeleteAsync(token);
    }

    private DateTimeOffset ResolveExpiration(DistributedCacheEntryOptions options, DateTimeOffset now)
    {
        if (options.AbsoluteExpiration is { } abs)
            return abs;
        if (options.AbsoluteExpirationRelativeToNow is { } rel)
            return now + rel;
        if (options.SlidingExpiration is { } sliding)
            return now + sliding;
        return now + opts.CurrentValue.Cache.DefaultTtl;
    }
}
