// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Infrastructure.Persistence;

namespace Bronnoysund.Lookup.MauiDesktop;

/// <summary>
/// MAUI database path provider — uses FileSystem.AppDataDirectory which is platform-correct
/// per host (iOS app sandbox, Android private storage, Mac/Win AppData).
/// </summary>
internal sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabaseFilePath()
        => Path.Combine(FileSystem.AppDataDirectory, "bronnoysund.db");
}
