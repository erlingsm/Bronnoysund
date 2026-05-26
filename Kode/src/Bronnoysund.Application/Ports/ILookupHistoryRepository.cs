// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Ports;

/// <summary>Snapshot of a previous lookup. ResultJson is raw CompanyResponse JSON (or null if not found).</summary>
public sealed record LookupHistoryItem(
    int Id,
    string? OrgNumber,
    string SearchTerm,
    DateTimeOffset SearchedAt,
    string? ResultJson,
    bool IsFavorite);

/// <summary>Search history. Non-favorites are deleted after retention; favorites are kept forever.</summary>
public interface ILookupHistoryRepository
{
    Task AddAsync(string? orgNumber, string searchTerm, string? resultJson, CancellationToken ct);
    Task<IReadOnlyList<LookupHistoryItem>> ListRecentAsync(int limit, CancellationToken ct);
    Task SetFavoriteAsync(int id, bool isFavorite, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task ClearNonFavoritesAsync(CancellationToken ct);
}
