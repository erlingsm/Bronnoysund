// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Page of voluntary-organization search hits. Frivillighetsregisteret paginates with a
/// cursor (<c>searchAfter</c>) rather than page numbers — <see cref="NextCursor"/> is
/// the opaque value the caller passes to the next request, or <c>null</c> when no
/// further pages are available.
/// </summary>
public sealed record VoluntaryOrganizationSearchResponse(
    IReadOnlyList<VoluntaryOrganizationResponse> Organizations,
    string? NextCursor);
