// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence;

namespace Bronnoysund.Lookup.MauiMobile;

/// <summary>
/// MAUI database-path provider — FileSystem.AppDataDirectory er iOS app-sandbox
/// (Library/Application Support på iOS, /data/data/&lt;pkg&gt;/files på Android).
/// </summary>
internal sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabaseFilePath()
        => Path.Combine(FileSystem.AppDataDirectory, "bronnoysund.db");
}
