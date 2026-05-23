// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Exceptions;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Stubs;

// Placeholder-implementasjoner for register-porter som ikke er implementert i Fase 0.
// Kaster RegistryNotAvailableException når kalt. Brukes som default-registreringer
// så DI-grafen er komplett. Erstattes med faktiske providers i Fase 4.

internal sealed class NotAvailableRolesProvider : IRolesProvider
{
    public Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Roller", "Implementeres i Fase 4. Krever ingen ekstern tilgang.");
}

internal sealed class NotAvailableAnnualReportProvider : IAnnualReportProvider
{
    public Task<AnnualReportResponse?> GetLatestAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Regnskap", "Krever Maskinporten-tilgang. Implementeres i Fase 4+ når avtale er på plass.");
}

internal sealed class NotAvailableBeneficialOwnerProvider : IBeneficialOwnerProvider
{
    public Task<BeneficialOwnersResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Reelle rettighetshavere", "Begrenset tilgang (AML/myndigheter). Implementeres i Fase 4+ ved tilgang.");
}

internal sealed class NotAvailableDebtRegisterProvider : IDebtRegisterProvider
{
    public Task<DebtSummaryResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Gjeldsregisteret", "Krever kommersiell avtale. Ikke åpent API.");
}

internal sealed class NotAvailableBankruptcyProvider : IBankruptcyProvider
{
    public Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Konkursregisteret", "Implementeres i Fase 4. Åpent API.");
}

internal sealed class NotAvailableSubUnitsProvider : ISubUnitsProvider
{
    public Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Underenheter", "Implementeres i Fase 4. Åpent API.");
}

internal sealed class NotAvailablePersonRolesProvider : IPersonRolesProvider
{
    public Task<PersonRolesResponse?> GetByPersonAsync(PersonIdentifier person, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Person-roller", "Implementeres i Fase 4. Åpent API via omvendt Roller-søk.");
}
