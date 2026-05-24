// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

/// <summary>
/// Cross-platform-port for hvor SQLite-fila skal lagres. Plattform-spesifikke
/// implementasjoner:
///   - MAUI: FileSystem.AppDataDirectory  (iOS app-sandbox, Android app-private, Mac/Win AppData)
///   - Server: SpecialFolder.LocalApplicationData
///   - WebApi (sky): konfigurerbar via appsettings (typisk /data/bronnoysund.db montert volum)
/// </summary>
public interface IDatabasePathProvider
{
    string GetDatabaseFilePath();
}

/// <summary>Default-impl for server-/WebApi-kontekst (ikke MAUI). Mac+Win+Linux.</summary>
public sealed class DefaultDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabaseFilePath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(dir, "Bronnoysund.Lookup");
        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, "bronnoysund.db");
    }
}
