// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Persistence.Entities;
using Bronnoysund.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bronnoysund.Infrastructure.Persistence.Tests;

public sealed class LookupHistoryRepositoryTests
{
    [Fact]
    public async Task AddAsync_StoresGzippedPayload_RoundTrippedOnRead()
    {
        using var test = new TestDb();
        var repo = new LookupHistoryRepository(test.Db, TestOptions.Of(new()));

        await repo.AddAsync("974760843", "974760843", "{\"name\":\"RIKSREVISJONEN\"}", default);

        var rows = await repo.ListRecentAsync(10, default);
        rows.Should().HaveCount(1);
        rows[0].OrgNumber.Should().Be("974760843");
        rows[0].ResultJson.Should().Be("{\"name\":\"RIKSREVISJONEN\"}");
        rows[0].IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task ListRecentAsync_SortsNewestFirst()
    {
        using var test = new TestDb();
        var repo = new LookupHistoryRepository(test.Db, TestOptions.Of(new()));

        await repo.AddAsync("111111111", "first", null, default);
        await Task.Delay(10);
        await repo.AddAsync("222222222", "second", null, default);

        var rows = await repo.ListRecentAsync(10, default);
        rows.Should().HaveCount(2);
        rows[0].OrgNumber.Should().Be("222222222");
        rows[1].OrgNumber.Should().Be("111111111");
    }

    [Fact]
    public async Task SetFavoriteAsync_UpdatesTheFlag()
    {
        using var test = new TestDb();
        var repo = new LookupHistoryRepository(test.Db, TestOptions.Of(new()));

        await repo.AddAsync("974760843", "974760843", null, default);
        var id = (await repo.ListRecentAsync(1, default))[0].Id;

        await repo.SetFavoriteAsync(id, true, default);
        var after = await repo.ListRecentAsync(1, default);
        after[0].IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task ClearNonFavoritesAsync_KeepsFavorites_DeletesOldNonFavorites()
    {
        using var test = new TestDb();
        var opts = new PersistenceOptions { History = new() { RetentionDays = 1 } };
        var repo = new LookupHistoryRepository(test.Db, TestOptions.Of(opts));

        test.Db.LookupHistory.AddRange(
            new LookupHistoryEntry
            {
                SearchTerm = "old-favorite",
                SearchedAt = DateTimeOffset.UtcNow - TimeSpan.FromDays(30),
                IsFavorite = true,
            },
            new LookupHistoryEntry
            {
                SearchTerm = "old-not-favorite",
                SearchedAt = DateTimeOffset.UtcNow - TimeSpan.FromDays(30),
                IsFavorite = false,
            },
            new LookupHistoryEntry
            {
                SearchTerm = "recent",
                SearchedAt = DateTimeOffset.UtcNow,
                IsFavorite = false,
            });
        await test.Db.SaveChangesAsync();

        await repo.ClearNonFavoritesAsync(default);

        var remaining = await test.Db.LookupHistory.OrderBy(e => e.SearchTerm).ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Select(r => r.SearchTerm).Should().BeEquivalentTo(["old-favorite", "recent"]);
    }
}
