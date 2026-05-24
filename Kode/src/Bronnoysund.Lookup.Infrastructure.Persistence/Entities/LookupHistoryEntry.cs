// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>
/// En historisk rad — hvert orgnr-oppslag som brukeren har gjort.
/// Ikke-favoritter slettes automatisk etter <c>Persistence:History:RetentionDays</c> dager.
/// </summary>
public sealed class LookupHistoryEntry
{
    public int Id { get; init; }
    public string? OrgNumber { get; init; }
    public required string SearchTerm { get; init; }
    public required DateTimeOffset SearchedAt { get; init; }
    public byte[]? ResultGzip { get; init; }
    public bool IsFavorite { get; init; }
}
