// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>Bookmark — company the user has marked as a favorite.</summary>
public sealed class FavoriteCompany
{
    public required string OrgNumber { get; init; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public required DateTimeOffset AddedAt { get; init; }
}
