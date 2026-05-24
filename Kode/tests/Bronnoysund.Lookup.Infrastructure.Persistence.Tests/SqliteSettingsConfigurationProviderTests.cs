// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SqliteSettingsConfigurationProviderTests
{
    [Fact]
    public async Task AddSqliteSettings_overstyrer_appsettings_json_verdier_etter_reload()
    {
        await using var harness = await SettingsRepositoryReloadHarness.CreateAsync(null!);

        // Bygg en config der appsettings.json sier https://default/ men SQLite-laget er på toppen.
        var inMemoryDefaults = new Dictionary<string, string?>
        {
            ["Brreg:BaseUrl"] = "https://default/",
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryDefaults)
            .AddSqliteSettings(() => harness.DbPath)
            .Build();

        // Før noe er skrevet til DB: defaultverdien gjelder.
        config["Brreg:BaseUrl"].Should().Be("https://default/");

        // Skriv via repo (som internt kaller harness.Configuration.Reload(), men vår config er en annen instans).
        await harness.Repo.SetAsync("Brreg:BaseUrl", "https://example.com/", "string", default);
        config.Reload();

        config["Brreg:BaseUrl"].Should().Be("https://example.com/");
    }

    [Fact]
    public async Task Load_returnerer_tom_data_naar_db_ikke_finnes()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"never-{Guid.NewGuid():N}.db");
        var config = new ConfigurationBuilder()
            .AddSqliteSettings(() => nonExistentPath)
            .Build();

        config["whatever"].Should().BeNull();
        await Task.CompletedTask;
    }
}
