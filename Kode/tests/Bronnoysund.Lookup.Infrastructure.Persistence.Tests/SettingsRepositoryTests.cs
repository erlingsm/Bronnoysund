// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SettingsRepositoryTests
{
    [Fact]
    public async Task SetAsync_oppretter_og_oppdaterer_eksisterende_med_ny_timestamp()
    {
        using var test = new TestDb();
        var repo = new SettingsRepository(test.Db);

        await repo.SetAsync("cache.maxSizeMB", "50", "int", default);
        var first = await repo.GetAsync("cache.maxSizeMB", default);
        first.Should().NotBeNull();
        first!.Value.Should().Be("50");

        await Task.Delay(5);
        await repo.SetAsync("cache.maxSizeMB", "100", "int", default);
        var second = await repo.GetAsync("cache.maxSizeMB", default);
        second!.Value.Should().Be("100");
        second.UpdatedAt.Should().BeAfter(first.UpdatedAt);
    }

    [Fact]
    public async Task GetAllAsync_returnerer_alle_settings_sortert()
    {
        using var test = new TestDb();
        var repo = new SettingsRepository(test.Db);

        await repo.SetAsync("z", "1", "int", default);
        await repo.SetAsync("a", "2", "int", default);

        var all = await repo.GetAllAsync(default);
        all.Select(s => s.Key).Should().Equal(["a", "z"]);
    }
}
