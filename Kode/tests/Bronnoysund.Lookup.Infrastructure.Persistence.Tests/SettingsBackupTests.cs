// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

public sealed class SettingsBackupTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task ExportAndImport_RoundTripsAllSettings()
    {
        using var sourceDb = new TestDb();
        var sourceRepo = new SettingsRepository(sourceDb.Db, EmptyConfig());
        await sourceRepo.SetAsync("Brreg:BaseUrl", "https://example.com/", "string", default);
        await sourceRepo.SetAsync("Persistence:Cache:MaxSizeMB", "100", "int", default);

        var sourceBackup = new SettingsBackup(sourceRepo);
        var json = await sourceBackup.ExportAsync(default);

        using var targetDb = new TestDb();
        var targetRepo = new SettingsRepository(targetDb.Db, EmptyConfig());
        var targetBackup = new SettingsBackup(targetRepo);
        await targetBackup.ImportAsync(json, default);

        var imported = await targetRepo.GetAllAsync(default);
        imported.Should().HaveCount(2);
        imported.Single(s => s.Key == "Brreg:BaseUrl").Value.Should().Be("https://example.com/");
        imported.Single(s => s.Key == "Persistence:Cache:MaxSizeMB").Value.Should().Be("100");
    }

    [Fact]
    public async Task Import_ThrowsOnInvalidJson()
    {
        using var db = new TestDb();
        var backup = new SettingsBackup(new SettingsRepository(db.Db, EmptyConfig()));

        await FluentActions
            .Invoking(() => backup.ImportAsync("{not valid", default))
            .Should().ThrowAsync<InvalidSettingsBackupException>();
    }

    [Fact]
    public async Task Import_ThrowsOnUnknownVersion()
    {
        using var db = new TestDb();
        var backup = new SettingsBackup(new SettingsRepository(db.Db, EmptyConfig()));

        var futureVersionJson = """{"Version":999,"ExportedAt":"2026-01-01T00:00:00Z","Settings":{}}""";
        await FluentActions
            .Invoking(() => backup.ImportAsync(futureVersionJson, default))
            .Should().ThrowAsync<InvalidSettingsBackupException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task Import_MergesWithExistingSettings()
    {
        using var db = new TestDb();
        var repo = new SettingsRepository(db.Db, EmptyConfig());
        await repo.SetAsync("eksisterende:nokkel", "behold-meg", "string", default);

        var backup = new SettingsBackup(repo);
        var json = """{"Version":1,"ExportedAt":"2026-01-01T00:00:00Z","Settings":{"ny:nokkel":{"Value":"42","DataType":"int"}}}""";
        await backup.ImportAsync(json, default);

        var all = await repo.GetAllAsync(default);
        all.Should().HaveCount(2);
        all.Select(s => s.Key).Should().Contain(["eksisterende:nokkel", "ny:nokkel"]);
    }
}
