// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>
/// Brukerinnstillinger som overstyrer appsettings.json — lever i SQLite.
/// Eksempel: Brreg-base-URL, cache TTL, max-size, history retention.
/// Endringer trigger IOptionsMonitor.OnChange uten app-restart.
/// </summary>
public sealed class AppSetting
{
    public required string Key { get; init; }       // PK, f.eks. "cache.maxSizeMB"
    public required string Value { get; set; }
    public required string DataType { get; init; }  // "string", "int", "bool", "double", "timespan"
    public required DateTimeOffset UpdatedAt { get; set; }
}
