// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Application.Ports;

// Aggregator-porter for parallelle oppslag mot flere offentlige registre.
// Detaljert spesifikasjon: /Plan/15-Register-aggregator.md
// I Fase 0 etableres kun interfacene; faktiske implementasjoner kommer i Fase 4
// med unntak av ICompanyProvider (kjerne) som har full impl allerede.

public interface IRolesProvider
{
    Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct);
}

public interface IAnnualReportProvider
{
    Task<AnnualReportResponse?> GetLatestAsync(OrganizationNumber org, CancellationToken ct);
}

public interface IBeneficialOwnerProvider
{
    Task<BeneficialOwnersResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}

public interface IDebtRegisterProvider
{
    Task<DebtSummaryResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}

public interface IBankruptcyProvider
{
    Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct);
}

public interface ISubUnitsProvider
{
    Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct);
}

public interface IPersonRolesProvider
{
    Task<PersonRolesResponse?> GetByPersonAsync(PersonIdentifier person, CancellationToken ct);
}

// Placeholder-records for fremtidige registre. Utvides i Fase 4 med faktiske felter.

public sealed record RolesResponse(IReadOnlyList<Role> Roles);
public sealed record Role(string PersonName, DateOnly? DateOfBirth, string RoleType);

public sealed record AnnualReportResponse(int Year, decimal? Revenue, decimal? Profit, decimal? Equity);

public sealed record BeneficialOwnersResponse(IReadOnlyList<BeneficialOwner> Owners);
public sealed record BeneficialOwner(string PersonName, decimal? OwnershipPercent);

public sealed record DebtSummaryResponse(decimal? TotalDebt, DateOnly AsOf);

public sealed record BankruptcyResponse(bool IsBankrupt, DateOnly? Declared);

public sealed record SubUnitsResponse(IReadOnlyList<SubUnit> SubUnits);
public sealed record SubUnit(string OrganizationNumber, string Name);

public sealed record PersonRolesResponse(IReadOnlyList<PersonCompanyInvolvement> Involvements);
public sealed record PersonCompanyInvolvement(string OrganizationNumber, string CompanyName, string RoleType);

public readonly record struct PersonIdentifier(string Name, DateOnly? DateOfBirth);
