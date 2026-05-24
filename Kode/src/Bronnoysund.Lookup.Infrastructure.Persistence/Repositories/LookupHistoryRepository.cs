// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.IO.Compression;
using System.Text;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Repositories;

internal sealed class LookupHistoryRepository(
    BronnoysundDbContext db,
    IOptionsMonitor<PersistenceOptions> opts) : ILookupHistoryRepository
{
    public async Task AddAsync(string? orgNumber, string searchTerm, string? resultJson, CancellationToken ct)
    {
        var entry = new LookupHistoryEntry
        {
            OrgNumber = orgNumber,
            SearchTerm = searchTerm,
            SearchedAt = DateTimeOffset.UtcNow,
            ResultGzip = resultJson is null ? null : Compress(resultJson),
            IsFavorite = false,
        };
        db.LookupHistory.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LookupHistoryItem>> ListRecentAsync(int limit, CancellationToken ct)
    {
        var rows = await db.LookupHistory
            .AsNoTracking()
            .OrderByDescending(e => e.SearchedAt)
            .Take(limit)
            .ToListAsync(ct);

        return rows.ConvertAll(e => new LookupHistoryItem(
            e.Id,
            e.OrgNumber,
            e.SearchTerm,
            e.SearchedAt,
            e.ResultGzip is null ? null : Decompress(e.ResultGzip),
            e.IsFavorite));
    }

    public async Task SetFavoriteAsync(int id, bool isFavorite, CancellationToken ct)
    {
        await db.LookupHistory
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsFavorite, isFavorite), ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await db.LookupHistory.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task ClearNonFavoritesAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow - TimeSpan.FromDays(opts.CurrentValue.History.RetentionDays);
        await db.LookupHistory
            .Where(e => !e.IsFavorite && e.SearchedAt < threshold)
            .ExecuteDeleteAsync(ct);
    }

    private static byte[] Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal))
        {
            gz.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    private static string Decompress(byte[] gzipped)
    {
        using var input = new MemoryStream(gzipped);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
