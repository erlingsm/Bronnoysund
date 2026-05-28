// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for a Frivillighetsregisteret search. Empty result list is still
/// <see cref="Found"/> with an empty list — distinct from <see cref="Unavailable"/>.
/// </summary>
public abstract record VoluntaryOrganizationSearchResult
{
    private VoluntaryOrganizationSearchResult() { }

    public sealed record Found(VoluntaryOrganizationSearchResponse Result) : VoluntaryOrganizationSearchResult;

    public sealed record InvalidInput(string Message) : VoluntaryOrganizationSearchResult;

    public sealed record Unavailable(string Message) : VoluntaryOrganizationSearchResult;
}
