// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Dtos;
using Bronnoysund.Lookup.Application.Results;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>
/// Orkestrerer parallelle oppslag mot flere offentlige registre for ett organisasjonsnummer.
/// Detaljert i /Plan/15-Register-aggregator.md.
/// </summary>
public interface ICompanyDataAggregator
{
    /// <summary>Hent all tilgjengelig info i parallell. Delvise svar mulig — feilende providers gir markører.</summary>
    Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, CancellationToken ct);

    /// <summary>Hent kun kjerne (Brreg Enhetsregisteret). Brukes der full aggregering ikke trengs.</summary>
    Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct);
}

/// <summary>
/// Aggregert respons med data fra flere registre. Hver nested-egenskap kan være null (ikke hentet/feilet).
/// Errors-listen indikerer per-provider feil.
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
