// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bronnoysund.Infrastructure.Persistence.Repositories;

internal sealed class FavoritesRepository(BronnoysundDbContext db) : IFavoritesRepository
{
    public async Task<FavoriteItem?> FindAsync(string orgNumber, CancellationToken ct)
    {
        var fav = await db.Favorites.AsNoTracking().FirstOrDefaultAsync(f => f.OrgNumber == orgNumber, ct);
        return fav is null ? null : new FavoriteItem(fav.OrgNumber, fav.Name, fav.Note, fav.AddedAt);
    }

    public async Task<IReadOnlyList<FavoriteItem>> ListAsync(CancellationToken ct)
    {
        var rows = await db.Favorites.AsNoTracking().OrderBy(f => f.Name).ToListAsync(ct);
        return rows.ConvertAll(f => new FavoriteItem(f.OrgNumber, f.Name, f.Note, f.AddedAt));
    }

    public async Task UpsertAsync(string orgNumber, string name, string? note, CancellationToken ct)
    {
        var existing = await db.Favorites.FindAsync([orgNumber], ct);
        if (existing is null)
        {
            db.Favorites.Add(new FavoriteCompany
            {
                OrgNumber = orgNumber,
                Name = name,
                Note = note,
                AddedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.Name = name;
            existing.Note = note;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string orgNumber, CancellationToken ct)
    {
        await db.Favorites.Where(f => f.OrgNumber == orgNumber).ExecuteDeleteAsync(ct);
    }
}
