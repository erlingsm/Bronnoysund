// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;

/// <summary>
/// IConfigurationSource som leser bruker-overstyrte settings fra AppSettings-tabellen.
/// Legges TIL etter appsettings.json så DB-verdier overstyrer fil-defaults.
///
/// Bruk via <see cref="ConfigurationBuilderExtensions.AddSqliteSettings"/>.
/// </summary>
public sealed class SqliteSettingsConfigurationSource : IConfigurationSource
{
    private readonly Func<string> _databasePathFactory;

    /// <summary>DB-path resolves lazily — providers bygges før IDatabasePathProvider er
    /// tilgjengelig i DI, så vi tar inn en factory som leser fra det som er klart da.</summary>
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
            // DI er ikke klart ennå (typisk ved første Load før app.Build()) — la base sin
            // tomme Data stå. Reload kalles senere fra SettingsRepository når DB finnes.
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
                // AppSettings bruker IConfiguration-syntax direkte (Brreg:BaseUrl, Persistence:Cache:MaxSizeMB).
                data[key] = value;
            }
        }
        catch (SqliteException)
        {
            // Tabellen finnes ikke ennå (EnsureCreated ikke kjørt). OK — Data forblir tom.
        }

        Data = data;
    }
}

public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Legger til SQLite-backed brukerinnstillinger på toppen av eksisterende sources.
    /// DB-pathen leveres som callback fordi vi typisk vil resolve den fra DI (IDatabasePathProvider)
    /// først etter at services er bygget.
    /// </summary>
    public static IConfigurationBuilder AddSqliteSettings(
        this IConfigurationBuilder builder,
        Func<string> databasePathFactory)
    {
        builder.Add(new SqliteSettingsConfigurationSource(databasePathFactory));
        return builder;
    }
}
