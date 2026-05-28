// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Aggregated change-log response for a single organization. Combines the three
/// Brreg "oppdateringer" feeds (entity, sub-unit, role) so the UI can render one
/// chronological history view per entity.
/// </summary>
/// <remarks>
/// Each feed carries its own error state — one failing feed should not hide the
/// other two. Mirrors the per-provider-error-isolation pattern of the aggregator.
/// </remarks>
public sealed record EntityChangesResponse(
    string OrganizationNumber,
    EntityChangesFeed EntityFeed,
    EntityChangesFeed SubUnitFeed,
    EntityChangesFeed RoleFeed);

/// <summary>One Brreg change-feed. <see cref="ErrorMessage"/> is non-null when this feed failed.</summary>
public sealed record EntityChangesFeed(
    IReadOnlyList<EntityChange> Changes,
    string? ErrorMessage = null);

/// <summary>One entry in a change feed. Sparse by design — Brreg's schemas vary across feeds.</summary>
public sealed record EntityChange(
    DateTimeOffset? Timestamp,
    string ChangeType,
    long? UpdateId);
