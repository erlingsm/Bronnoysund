// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Aggregation;

/// <summary>
/// Aggregator implementation for Phase 0 — only calls <see cref="ICompanyProvider"/> (the core).
/// Phase 4 introduces ParallelCompanyDataAggregator which calls all providers in parallel.
/// Details: /Plan/15-Register-aggregator.md
/// </summary>
internal sealed class CoreOnlyAggregator(ICompanyProvider company) : ICompanyDataAggregator
{
    public async Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct)
    {
        var result = await company.LookupAsync(org, ct);
        if (result is CompanyLookupResult.Found found)
        {
            return new AggregatedCompanyResponse(
                Core: found.Company,
                Roles: null,
                LatestAnnualReport: null,
                BeneficialOwners: null,
                Debt: null,
                Bankruptcy: null,
                SubUnits: null,
                Errors: [new RegistryError("aggregator", "Phase 0: only the core registry is implemented. More registries arrive in Phase 4.")]
            );
        }

        throw new InvalidOperationException(
            $"AggregateAsync requires a Found result. Got {result.GetType().Name}. " +
            "Call CoreOnlyAsync first to check the status.");
    }

    public Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct)
        => company.LookupAsync(org, ct);
}
