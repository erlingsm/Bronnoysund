// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence;

/// <summary>
/// Cross-platform port for where the SQLite file should be stored. Platform-specific
/// implementations:
///   - MAUI: FileSystem.AppDataDirectory  (iOS app sandbox, Android app-private, Mac/Win AppData)
///   - Server: SpecialFolder.LocalApplicationData
///   - WebApi (cloud): override via BRONNOYSUND_DB_PATH env-var (typically a mounted volume)
/// </summary>
public interface IDatabasePathProvider
{
    string GetDatabaseFilePath();
}

/// <summary>
/// Default implementation for server / WebApi contexts (non-MAUI). Mac+Win+Linux.
/// In container environments the LocalApplicationData fallback may resolve to a path
/// the process user cannot write to. Set BRONNOYSUND_DB_PATH to an absolute file path
/// the container has write access to (e.g. /tmp/bronnoysund.db or a mounted volume).
/// </summary>
public sealed class DefaultDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabaseFilePath()
    {
        var explicitPath = Environment.GetEnvironmentVariable("BRONNOYSUND_DB_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var explicitDir = Path.GetDirectoryName(explicitPath);
            if (!string.IsNullOrEmpty(explicitDir))
            {
                Directory.CreateDirectory(explicitDir);
            }
            return explicitPath;
        }

        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(dir, "Bronnoysund.Lookup");
        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, "bronnoysund.db");
    }
}
