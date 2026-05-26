// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Infrastructure.Persistence.Configuration;

/// <summary>
/// IConfigurationSource that reads user-overridden settings from the AppSettings table.
/// Added AFTER appsettings.json so DB values override the file defaults.
///
/// Use via <see cref="ConfigurationBuilderExtensions.AddSqliteSettings"/>.
/// </summary>
public sealed class SqliteSettingsConfigurationSource : IConfigurationSource
{
    private readonly Func<string> _databasePathFactory;

    /// <summary>DB path resolves lazily — providers are built before IDatabasePathProvider is
    /// available in DI, so we take a factory that reads from whatever is ready at that point.</summary>
    public SqliteSettingsConfigurationSource(Func<string> databasePathFactory)
    {
        _databasePathFactory = databasePathFactory;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SqliteSettingsConfigurationProvider(_databasePathFactory);
}

public sealed class SqliteSettingsConfigurationProvider(Func<string> databasePathFactory) : ConfigurationProvider
{
    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        string? dbPath;
        try
        {
            dbPath = databasePathFactory();
        }
        catch
        {
            // DI is not ready yet (typically on the first Load before app.Build()) — leave base's
            // empty Data in place. Reload is called later from SettingsRepository once the DB exists.
            Data = data;
            return;
        }

        if (!File.Exists(dbPath))
        {
            Data = data;
            return;
        }

        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM AppSettings";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);
                // AppSettings uses IConfiguration syntax directly (Brreg:BaseUrl, Persistence:Cache:MaxSizeMB).
                data[key] = value;
            }
        }
        catch (SqliteException)
        {
            // The table does not exist yet (EnsureCreated has not run). OK — Data stays empty.
        }

        Data = data;
    }
}

public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds SQLite-backed user settings on top of the existing sources.
    /// The DB path is supplied as a callback because we typically want to resolve it from DI
    /// (IDatabasePathProvider) only after services have been built.
    /// </summary>
    public static IConfigurationBuilder AddSqliteSettings(
        this IConfigurationBuilder builder,
        Func<string> databasePathFactory)
    {
        builder.Add(new SqliteSettingsConfigurationSource(databasePathFactory));
        return builder;
    }
}
