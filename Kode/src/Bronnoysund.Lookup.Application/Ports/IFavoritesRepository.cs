// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

public sealed record FavoriteItem(string OrgNumber, string Name, string? Note, DateTimeOffset AddedAt);

public interface IFavoritesRepository
{
    Task<FavoriteItem?> FindAsync(string orgNumber, CancellationToken ct);
    Task<IReadOnlyList<FavoriteItem>> ListAsync(CancellationToken ct);
    Task UpsertAsync(string orgNumber, string name, string? note, CancellationToken ct);
    Task RemoveAsync(string orgNumber, CancellationToken ct);
}
