// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Aggregation;

/// <summary>
/// Aggregator implementation for Phase 0 — only calls the Norwegian <see cref="ICompanyProvider"/>
/// (the core). Routes through <see cref="ICompanyProviderRegistry"/> because resolving the bare
/// <see cref="ICompanyProvider"/> picks the last-registered implementation in DI, which is one
/// of the international Plan 21 providers after Bølge 1–4 landed. Phase 4 introduces
/// ParallelCompanyDataAggregator which calls all providers in parallel.
/// Details: /Plan/15-Register-aggregator.md
/// </summary>
internal sealed class CoreOnlyAggregator(ICompanyProviderRegistry registry) : ICompanyDataAggregator
{
    private ICompanyProvider Company =>
        registry.GetForCountry("NO")
            ?? throw new InvalidOperationException("Norwegian ICompanyProvider is not registered.");

    public async Task<AggregatedCompanyResponse> AggregateAsync(
        OrganizationNumber org,
        AggregatedScope scope,
        CancellationToken ct)
    {
        var result = await Company.LookupAsync(org, ct);
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
        => Company.LookupAsync(org, ct);
}
