// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

public sealed record AppSettingItem(string Key, string Value, string DataType, DateTimeOffset UpdatedAt);

/// <summary>
/// Bruker-overstyrte settings (Brreg-URL, cache-størrelse, retention osv).
/// Endring trigger IOptionsMonitor.OnChange via SqliteSettingsConfigurationProvider.
/// </summary>
public interface ISettingsRepository
{
    Task<AppSettingItem?> GetAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<AppSettingItem>> GetAllAsync(CancellationToken ct);
    Task SetAsync(string key, string value, string dataType, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}
