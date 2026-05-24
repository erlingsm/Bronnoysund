// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>Snapshot av et tidligere oppslag. ResultJson er rå CompanyResponse JSON (eller null hvis ikke funnet).</summary>
public sealed record LookupHistoryItem(
    int Id,
    string? OrgNumber,
    string SearchTerm,
    DateTimeOffset SearchedAt,
    string? ResultJson,
    bool IsFavorite);

/// <summary>Søkehistorikk. Ikke-favoritter slettes etter retention; favoritter beholdes for alltid.</summary>
public interface ILookupHistoryRepository
{
    Task AddAsync(string? orgNumber, string searchTerm, string? resultJson, CancellationToken ct);
    Task<IReadOnlyList<LookupHistoryItem>> ListRecentAsync(int limit, CancellationToken ct);
    Task SetFavoriteAsync(int id, bool isFavorite, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task ClearNonFavoritesAsync(CancellationToken ct);
}
