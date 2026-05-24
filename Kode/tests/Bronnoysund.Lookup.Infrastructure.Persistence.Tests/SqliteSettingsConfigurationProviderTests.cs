// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SqliteSettingsConfigurationProviderTests
{
    [Fact]
    public async Task AddSqliteSettings_OverridesAppSettingsJsonValuesAfterReload()
    {
        await using var harness = await SettingsRepositoryReloadHarness.CreateAsync(null!);

        // Build a config where appsettings.json says https://default/ but the SQLite layer is on top.
        var inMemoryDefaults = new Dictionary<string, string?>
        {
            ["Brreg:BaseUrl"] = "https://default/",
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryDefaults)
            .AddSqliteSettings(() => harness.DbPath)
            .Build();

        // Before anything is written to the DB: the default value applies.
        config["Brreg:BaseUrl"].Should().Be("https://default/");

        // Write via the repo (which internally calls harness.Configuration.Reload(), but our config is a different instance).
        await harness.Repo.SetAsync("Brreg:BaseUrl", "https://example.com/", "string", default);
        config.Reload();

        config["Brreg:BaseUrl"].Should().Be("https://example.com/");
    }

    [Fact]
    public async Task Load_ReturnsEmptyData_WhenDbDoesNotExist()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"never-{Guid.NewGuid():N}.db");
        var config = new ConfigurationBuilder()
            .AddSqliteSettings(() => nonExistentPath)
            .Build();

        config["whatever"].Should().BeNull();
        await Task.CompletedTask;
    }
}
