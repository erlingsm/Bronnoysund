// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Aggregation;

/// <summary>
/// Aggregator-implementasjon for Fase 0 — kaller kun <see cref="ICompanyProvider"/> (kjernen).
/// Fase 4 introduserer ParallelCompanyDataAggregator som kaller alle providers parallelt.
/// Detaljer: /Plan/15-Register-aggregator.md
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
                Errors: [new RegistryError("aggregator", "Fase 0: kun kjerne implementert. Flere registre kommer i Fase 4.")]
            );
        }

        throw new InvalidOperationException(
            $"AggregateAsync krever et Found-resultat. Fikk {result.GetType().Name}. " +
            "Kall CoreOnlyAsync først for å sjekke status.");
    }

    public Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct)
        => company.LookupAsync(org, ct);
}
