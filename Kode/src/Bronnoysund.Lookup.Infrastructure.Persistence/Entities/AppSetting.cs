// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>
/// User settings that override appsettings.json — live in SQLite.
/// Example: Brreg base URL, cache TTL, max size, history retention.
/// Changes trigger IOptionsMonitor.OnChange without an app restart.
/// </summary>
public sealed class AppSetting
{
    public required string Key { get; init; }       // PK, e.g. "cache.maxSizeMB"
    public required string Value { get; set; }
    public required string DataType { get; init; }  // "string", "int", "bool", "double", "timespan"
    public required DateTimeOffset UpdatedAt { get; set; }
}
