// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;

namespace Bronnoysund.Application.Results;

/// <summary>
/// Discriminated union for direct sub-unit lookup results. Mirrors
/// <see cref="CompanyLookupResult"/> — Brreg returns 200 OK with a thin payload when
/// the sub-unit is soft-deleted (mapped to <see cref="NotFound"/>) and 410 Gone when
/// it has been removed entirely (also <see cref="NotFound"/>).
/// </summary>
public abstract record SubUnitLookupResult
{
    private SubUnitLookupResult() { }

    public sealed record Found(SubUnitDetailsResponse SubUnit) : SubUnitLookupResult;

    public sealed record NotFound(string OrganizationNumber) : SubUnitLookupResult;

    public sealed record Unavailable(string Message) : SubUnitLookupResult;
}
