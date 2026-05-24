// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Cache;

/// <summary>
/// 1) Slett utløpte cache-entries.
/// 2) Hvis totalstørrelse &gt; MaxSizeMB: slett LRU-entries til vi er under 90 % av maks.
/// Kjøres periodisk av PersistenceMaintenanceHostedService.
/// </summary>
internal sealed class CacheEvictionService(
    BronnoysundDbContext db,
    IOptionsMonitor<PersistenceOptions> opts,
    ILogger<CacheEvictionService> log)
{
    public async Task EvictIfNeededAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var expired = await db.CacheEntries
            .Where(c => c.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);
        if (expired > 0)
            log.LogInformation("Slettet {Count} utløpte cache-entries", expired);

        var maxBytes = opts.CurrentValue.Cache.MaxSizeMB * 1024L * 1024L;
        var target = (long)(maxBytes * 0.9);

        var totalSize = await db.CacheEntries.SumAsync(c => (long)c.SizeBytes, ct);
        if (totalSize <= maxBytes)
            return;

        var oldest = await db.CacheEntries
            .OrderBy(c => c.LastAccessedAt)
            .Select(c => new { c.Key, c.SizeBytes })
            .ToListAsync(ct);

        var runningTotal = totalSize;
        var toDelete = new List<string>();
        foreach (var entry in oldest)
        {
            if (runningTotal <= target) break;
            toDelete.Add(entry.Key);
            runningTotal -= entry.SizeBytes;
        }

        if (toDelete.Count == 0)
            return;

        var deleted = await db.CacheEntries
            .Where(c => toDelete.Contains(c.Key))
            .ExecuteDeleteAsync(ct);
        log.LogInformation(
            "LRU-evict: {Deleted} entries fjernet ({Before} → {After} bytes, max {Max})",
            deleted, totalSize, runningTotal, maxBytes);
    }
}
