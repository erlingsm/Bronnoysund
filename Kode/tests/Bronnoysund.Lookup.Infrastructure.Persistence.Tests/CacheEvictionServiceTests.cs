// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Cache;
using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class CacheEvictionServiceTests
{
    [Fact]
    public async Task EvictIfNeededAsync_DeletesExpiredEntries()
    {
        using var test = new TestDb();
        var opts = new PersistenceOptions { Cache = new() { MaxSizeMB = 100 } };
        var svc = new CacheEvictionService(test.Db, TestOptions.Of(opts), NullLogger<CacheEvictionService>.Instance);

        var now = DateTimeOffset.UtcNow;
        test.Db.CacheEntries.AddRange(
            Entry("expired", [1], expiresAt: now - TimeSpan.FromMinutes(1)),
            Entry("alive", [1], expiresAt: now + TimeSpan.FromHours(1)));
        await test.Db.SaveChangesAsync();

        await svc.EvictIfNeededAsync(default);

        var remaining = await test.Db.CacheEntries.Select(c => c.Key).ToListAsync();
        remaining.Should().Equal(["alive"]);
    }

    [Fact]
    public async Task EvictIfNeededAsync_LRU_DeletesOldestUntilUnder90PercentOfMax()
    {
        using var test = new TestDb();
        // 1 KB max → target = 921 bytes. Each entry is ~500 bytes payload.
        var opts = new PersistenceOptions { Cache = new() { MaxSizeMB = 0 } };
        // We set MaxSizeMB=0 so max = 0 bytes → any entry exceeds it and the entire cache is evicted.
        var svc = new CacheEvictionService(test.Db, TestOptions.Of(opts), NullLogger<CacheEvictionService>.Instance);

        var now = DateTimeOffset.UtcNow;
        test.Db.CacheEntries.AddRange(
            Entry("oldest", new byte[500], lastAccessed: now - TimeSpan.FromHours(3)),
            Entry("mid",    new byte[500], lastAccessed: now - TimeSpan.FromHours(2)),
            Entry("newest", new byte[500], lastAccessed: now - TimeSpan.FromHours(1)));
        await test.Db.SaveChangesAsync();

        await svc.EvictIfNeededAsync(default);

        var remaining = await test.Db.CacheEntries.Select(c => c.Key).ToListAsync();
        remaining.Should().BeEmpty(); // max=0 evicts everything
    }

    [Fact]
    public async Task EvictIfNeededAsync_KeepsNewestWhenOnlyOldestNeedToBeEvicted()
    {
        using var test = new TestDb();
        // 1 MB max → target = ~944 KB. Each entry is 500 KB. Three entries = 1500 KB > 1 MB.
        var opts = new PersistenceOptions { Cache = new() { MaxSizeMB = 1 } };
        var svc = new CacheEvictionService(test.Db, TestOptions.Of(opts), NullLogger<CacheEvictionService>.Instance);

        var now = DateTimeOffset.UtcNow;
        test.Db.CacheEntries.AddRange(
            Entry("oldest", new byte[500_000], lastAccessed: now - TimeSpan.FromHours(3)),
            Entry("mid",    new byte[500_000], lastAccessed: now - TimeSpan.FromHours(2)),
            Entry("newest", new byte[500_000], lastAccessed: now - TimeSpan.FromHours(1)));
        await test.Db.SaveChangesAsync();

        await svc.EvictIfNeededAsync(default);

        var remaining = await test.Db.CacheEntries.OrderBy(c => c.Key).Select(c => c.Key).ToListAsync();
        remaining.Should().NotContain("oldest");
        remaining.Should().Contain("newest");
    }

    private static CacheEntry Entry(string key, byte[] value, DateTimeOffset? expiresAt = null, DateTimeOffset? lastAccessed = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CacheEntry
        {
            Key = key,
            Value = value,
            SizeBytes = value.Length,
            StoredAt = now,
            ExpiresAt = expiresAt ?? now + TimeSpan.FromHours(1),
            LastAccessedAt = lastAccessed ?? now,
        };
    }
}
