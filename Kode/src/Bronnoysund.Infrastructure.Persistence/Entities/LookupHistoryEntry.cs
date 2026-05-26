// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Persistence.Entities;

/// <summary>
/// A history row — every organization-number lookup the user has performed.
/// Non-favorites are automatically deleted after <c>Persistence:History:RetentionDays</c> days.
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
