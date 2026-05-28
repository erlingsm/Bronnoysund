// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Orchestrates parallel lookups against multiple public registries for one organization number.
/// Detailed in /Plan/15-Register-aggregator.md.
/// </summary>
public interface ICompanyDataAggregator
{
    /// <summary>
    /// Fetch all available info in parallel. Partial responses are allowed — failing providers
    /// leave markers in <see cref="AggregatedCompanyResponse.Errors"/>. The <paramref name="scope"/>
    /// controls whether the heavier enrichment providers (sub-unit details, legal roles, voluntary
    /// status, change feed) are fanned out — default keeps the original CoreOnly behaviour so
    /// existing WebApi callers do not pay for data they never read.
    /// </summary>
    Task<AggregatedCompanyResponse> AggregateAsync(
        OrganizationNumber org,
        AggregatedScope scope,
        CancellationToken ct);

    /// <summary>Convenience overload defaulting to <see cref="AggregatedScope.CoreOnly"/>.</summary>
    Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct)
        => AggregateAsync(org, AggregatedScope.CoreOnly, ct);

    /// <summary>Fetch only the core (Brreg Enhetsregisteret). Used where full aggregation is not needed.</summary>
    Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct);
}

/// <summary>
/// Controls which providers <see cref="ICompanyDataAggregator.AggregateAsync(OrganizationNumber, AggregatedScope, CancellationToken)"/>
/// fans out to. <see cref="CoreOnly"/> matches the legacy behaviour (the cheap, always-available
/// providers); <see cref="IncludeEnrichment"/> adds the four per-entity providers introduced
/// in iteration 8. <see cref="Full"/> is reserved for future expansion and currently behaves
/// like <see cref="IncludeEnrichment"/>.
/// </summary>
public enum AggregatedScope
{
    CoreOnly = 0,
    IncludeEnrichment = 1,
    Full = 2,
}

/// <summary>
/// Aggregated response with data from multiple registries. Each nested property may be null
/// (not fetched / failed). The <see cref="Errors"/> list indicates per-provider failures.
/// The four enrichment properties (<see cref="SubUnitDetails"/>, <see cref="LegalRoles"/>,
/// <see cref="Voluntary"/>, <see cref="Changes"/>) stay null unless the caller asked for
/// <see cref="AggregatedScope.IncludeEnrichment"/> or higher.
/// </summary>
public sealed record AggregatedCompanyResponse(
    CompanyResponse Core,
    RolesResponse? Roles,
    AnnualReportResponse? LatestAnnualReport,
    BeneficialOwnersResponse? BeneficialOwners,
    DebtSummaryResponse? Debt,
    BankruptcyResponse? Bankruptcy,
    SubUnitsResponse? SubUnits,
    IReadOnlyList<RegistryError> Errors,
    SubUnitDetailsResponse? SubUnitDetails = null,
    LegalRolesResponse? LegalRoles = null,
    VoluntaryOrganizationResponse? Voluntary = null,
    EntityChangesResponse? Changes = null
);

public sealed record RegistryError(string RegistryName, string Message);
