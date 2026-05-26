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
    /// <summary>Fetch all available info in parallel. Partial responses are allowed — failing providers leave markers.</summary>
    Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct);

    /// <summary>Fetch only the core (Brreg Enhetsregisteret). Used where full aggregation is not needed.</summary>
    Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct);
}

/// <summary>
/// Aggregated response with data from multiple registries. Each nested property may be null (not fetched / failed).
/// The Errors list indicates per-provider failures.
/// </summary>
public sealed record AggregatedCompanyResponse(
    CompanyResponse Core,
    RolesResponse? Roles,
    AnnualReportResponse? LatestAnnualReport,
    BeneficialOwnersResponse? BeneficialOwners,
    DebtSummaryResponse? Debt,
    BankruptcyResponse? Bankruptcy,
    SubUnitsResponse? SubUnits,
    IReadOnlyList<RegistryError> Errors
);

public sealed record RegistryError(string RegistryName, string Message);
