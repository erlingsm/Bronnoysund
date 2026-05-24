// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;
using Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Tests;

/// <summary>
/// Set up a real (file-based) SQLite + an IConfigurationRoot with SqliteSettingsConfigurationProvider,
/// so we can verify that SetAsync.Reload() is picked up by the config layer. In-memory ":memory:"
/// does not work here because the configuration provider opens its own connection.
/// </summary>
internal sealed class SettingsRepositoryReloadHarness : IAsyncDisposable
{
    public string DbPath { get; }
    public IConfigurationRoot Configuration { get; }
    public SettingsRepository Repo { get; }
    public BronnoysundDbContext Db { get; }

    private SettingsRepositoryReloadHarness(string dbPath, IConfigurationRoot config, BronnoysundDbContext db, SettingsRepository repo)
    {
        DbPath = dbPath;
        Configuration = config;
        Db = db;
        Repo = repo;
    }

    public static async Task<SettingsRepositoryReloadHarness> CreateAsync(BronnoysundDbContext _)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"bronnoysund-test-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<BronnoysundDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var db = new BronnoysundDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var config = new ConfigurationBuilder()
            .AddSqliteSettings(() => dbPath)
            .Build();

        var repo = new SettingsRepository(db, config);
        return new SettingsRepositoryReloadHarness(dbPath, config, db, repo);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(DbPath))
            File.Delete(DbPath);
    }
}
