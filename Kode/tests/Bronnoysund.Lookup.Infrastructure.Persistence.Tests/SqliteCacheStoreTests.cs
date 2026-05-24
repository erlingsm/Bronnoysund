// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Cache;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SqliteCacheStoreTests
{
    [Fact]
    public async Task SetAsync_Stores_AndGetAsync_ReturnsSameBytes()
    {
        await using var fixture = new CacheFixture();

        var payload = new byte[] { 1, 2, 3, 4, 5 };
        await fixture.Cache.SetAsync("key1", payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        });

        var read = await fixture.Cache.GetAsync("key1");
        read.Should().Equal(payload);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_ForExpiredEntry()
    {
        await using var fixture = new CacheFixture();

        await fixture.Cache.SetAsync("expired", [9], new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50),
        });
        await Task.Delay(100);

        var read = await fixture.Cache.GetAsync("expired");
        read.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UpdatesLastAccessedAt()
    {
        await using var fixture = new CacheFixture();

        await fixture.Cache.SetAsync("k", [1], new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        });
        var initial = await fixture.LastAccessedAtAsync("k");

        await Task.Delay(20);
        _ = await fixture.Cache.GetAsync("k");
        var afterRead = await fixture.LastAccessedAtAsync("k");

        afterRead.Should().BeAfter(initial);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry()
    {
        await using var fixture = new CacheFixture();

        await fixture.Cache.SetAsync("k", [1], new DistributedCacheEntryOptions());
        await fixture.Cache.RemoveAsync("k");

        (await fixture.Cache.GetAsync("k")).Should().BeNull();
    }
}

/// <summary>
/// In-memory SQLite + ServiceProvider with a scoped BronnoysundDbContext so that SqliteCacheStore
/// can CreateScope() the same way as in prod.
/// </summary>
internal sealed class CacheFixture : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    public IDistributedCache Cache { get; }

    public CacheFixture()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var collection = new ServiceCollection();
        collection.AddSingleton(connection);
        collection.AddDbContext<BronnoysundDbContext>(opts => opts.UseSqlite(connection));
        collection.AddSingleton<IDistributedCache, SqliteCacheStore>();
        collection.AddOptions<PersistenceOptions>();

        _services = collection.BuildServiceProvider();

        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();
            db.Database.EnsureCreated();
        }

        Cache = _services.GetRequiredService<IDistributedCache>();
    }

    public async Task<DateTimeOffset> LastAccessedAtAsync(string key)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BronnoysundDbContext>();
        var entry = await db.CacheEntries.AsNoTracking().FirstAsync(e => e.Key == key);
        return entry.LastAccessedAt;
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
    }
}
