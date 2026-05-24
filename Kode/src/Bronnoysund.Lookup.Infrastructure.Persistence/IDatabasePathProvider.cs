// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

/// <summary>
/// Cross-platform port for where the SQLite file should be stored. Platform-specific
/// implementations:
///   - MAUI: FileSystem.AppDataDirectory  (iOS app sandbox, Android app-private, Mac/Win AppData)
///   - Server: SpecialFolder.LocalApplicationData
///   - WebApi (cloud): configurable via appsettings (typically a mounted /data/bronnoysund.db volume)
/// </summary>
public interface IDatabasePathProvider
{
    string GetDatabaseFilePath();
}

/// <summary>Default implementation for server / WebApi contexts (non-MAUI). Mac+Win+Linux.</summary>
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
