// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Provider for <c>/frivillighetsregisteret/api/frivillige-organisasjoner</c> — a paginated
/// listing of voluntary organizations. Page size and cursor are caller-supplied; cursor-based
/// pagination matches Brreg's <c>searchAfter</c> contract for forward-only iteration.
/// </summary>
public interface IVoluntaryOrganizationSearchProvider
{
    Task<VoluntaryOrganizationSearchResult> SearchAsync(VoluntaryOrganizationSearchQuery query, CancellationToken ct);
}

/// <summary>
/// Search query for the voluntary-organization listing. <see cref="Size"/> clamps to Brreg's
/// 1–100 server-side limit (we enforce client-side to fail fast). <see cref="SearchAfter"/> is
/// the opaque cursor returned from the previous page, or <c>null</c> to start at the beginning.
/// </summary>
public sealed record VoluntaryOrganizationSearchQuery(int Size = 20, string? SearchAfter = null);
