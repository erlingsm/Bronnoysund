// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Maintenance;

/// <summary>Deletes non-favorite history older than RetentionDays.</summary>
internal sealed class HistoryCleanupService(
    BronnoysundDbContext db,
    IOptionsMonitor<PersistenceOptions> opts,
    ILogger<HistoryCleanupService> log)
{
    public async Task CleanupAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow - TimeSpan.FromDays(opts.CurrentValue.History.RetentionDays);
        var deleted = await db.LookupHistory
            .Where(h => !h.IsFavorite && h.SearchedAt < threshold)
            .ExecuteDeleteAsync(ct);
        if (deleted > 0)
            log.LogInformation("History cleanup: deleted {Count} non-favorite entries older than {Threshold:O}", deleted, threshold);
    }
}
