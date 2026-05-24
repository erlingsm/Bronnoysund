// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SettingsRepositoryTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task SetAsync_oppretter_og_oppdaterer_eksisterende_med_ny_timestamp()
    {
        using var test = new TestDb();
        var repo = new SettingsRepository(test.Db, EmptyConfig());

        await repo.SetAsync("Persistence:Cache:MaxSizeMB", "50", "int", default);
        var first = await repo.GetAsync("Persistence:Cache:MaxSizeMB", default);
        first.Should().NotBeNull();
        first!.Value.Should().Be("50");

        await Task.Delay(5);
        await repo.SetAsync("Persistence:Cache:MaxSizeMB", "100", "int", default);
        var second = await repo.GetAsync("Persistence:Cache:MaxSizeMB", default);
        second!.Value.Should().Be("100");
        second.UpdatedAt.Should().BeAfter(first.UpdatedAt);
    }

    [Fact]
    public async Task GetAllAsync_returnerer_alle_settings_sortert()
    {
        using var test = new TestDb();
        var repo = new SettingsRepository(test.Db, EmptyConfig());

        await repo.SetAsync("z", "1", "int", default);
        await repo.SetAsync("a", "2", "int", default);

        var all = await repo.GetAllAsync(default);
        all.Select(s => s.Key).Should().Equal(["a", "z"]);
    }

    [Fact]
    public async Task SetAsync_trigger_IConfigurationRoot_Reload_slik_at_options_oppdateres()
    {
        using var test = new TestDb();
        await using var session = await SettingsRepositoryReloadHarness.CreateAsync(test.Db);

        await session.Repo.SetAsync("Brreg:BaseUrl", "https://example.com/", "string", default);

        session.Configuration["Brreg:BaseUrl"].Should().Be("https://example.com/");
    }
}
