// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>
/// Export/import of user settings to move between devices.
/// The JSON format is versioned so future schema changes can be migrated.
/// </summary>
public interface ISettingsBackup
{
    /// <summary>Returns all settings as a JSON payload ready for file storage.</summary>
    Task<string> ExportAsync(CancellationToken ct);

    /// <summary>
    /// Reads the payload and upserts all keys. Existing keys not mentioned in the payload
    /// are retained (merge). Throws <see cref="InvalidSettingsBackupException"/> on invalid JSON
    /// or unknown version.
    /// </summary>
    Task ImportAsync(string json, CancellationToken ct);
}

public sealed class InvalidSettingsBackupException(string message) : Exception(message);
