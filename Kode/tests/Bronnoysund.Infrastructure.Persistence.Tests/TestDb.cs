// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Persistence.Tests;

/// <summary>
/// Helper for per-test in-memory SQLite — each test gets an isolated DB.
/// The connection is kept open for the entire test lifetime so :memory: is not reset.
/// </summary>
internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public BronnoysundDbContext Db { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<BronnoysundDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new BronnoysundDbContext(opts);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

internal static class TestOptions
{
    public static IOptionsMonitor<PersistenceOptions> Of(PersistenceOptions opts)
        => new StaticMonitor(opts);

    private sealed class StaticMonitor(PersistenceOptions opts) : IOptionsMonitor<PersistenceOptions>
    {
        public PersistenceOptions CurrentValue => opts;
        public PersistenceOptions Get(string? name) => opts;
        public IDisposable? OnChange(Action<PersistenceOptions, string?> listener) => null;
    }
}
