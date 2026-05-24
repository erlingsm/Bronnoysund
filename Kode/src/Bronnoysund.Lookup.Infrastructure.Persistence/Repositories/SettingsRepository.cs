// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;

internal sealed class SettingsRepository(
    BronnoysundDbContext db,
    IConfiguration configuration) : ISettingsRepository
{
    public async Task<AppSettingItem?> GetAsync(string key, CancellationToken ct)
    {
        var s = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);
        return s is null ? null : new AppSettingItem(s.Key, s.Value, s.DataType, s.UpdatedAt);
    }

    public async Task<IReadOnlyList<AppSettingItem>> GetAllAsync(CancellationToken ct)
    {
        var rows = await db.AppSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync(ct);
        return rows.ConvertAll(s => new AppSettingItem(s.Key, s.Value, s.DataType, s.UpdatedAt));
    }

    public async Task SetAsync(string key, string value, string dataType, CancellationToken ct)
    {
        var existing = await db.AppSettings.FindAsync([key], ct);
        if (existing is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                DataType = dataType,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        TriggerConfigReload();
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        await db.AppSettings.Where(s => s.Key == key).ExecuteDeleteAsync(ct);
        TriggerConfigReload();
    }

    /// <summary>
    /// Trigger IConfigurationRoot.Reload() slik at SqliteSettingsConfigurationProvider
    /// leser tabellen på nytt og IOptionsMonitor.OnChange fyrer for alle bundne options.
    /// No-op hvis IConfiguration ikke er en IConfigurationRoot (testing).
    /// </summary>
    private void TriggerConfigReload()
    {
        if (configuration is IConfigurationRoot root)
            root.Reload();
    }
}
