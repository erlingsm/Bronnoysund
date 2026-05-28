// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for a Frivillighetsregisteret lookup. <see cref="NotRegistered"/>
/// is distinct from <see cref="Unavailable"/>: the former means "we asked and got 404,
/// the org is not a voluntary organization" (a meaningful business answer), while the
/// latter means we could not reach Frivillighetsregisteret.
/// </summary>
public abstract record VoluntaryOrganizationLookupResult
{
    private VoluntaryOrganizationLookupResult() { }

    public sealed record Found(VoluntaryOrganizationResponse Organization) : VoluntaryOrganizationLookupResult;

    public sealed record NotRegistered(string OrganizationNumber) : VoluntaryOrganizationLookupResult;

    public sealed record Unavailable(string Message) : VoluntaryOrganizationLookupResult;
}
