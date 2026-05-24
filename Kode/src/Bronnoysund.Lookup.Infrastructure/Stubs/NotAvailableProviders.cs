// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Exceptions;
using Bronnoysund.Lookup.Application.Ports;
using Bronnoysund.Lookup.Domain;

namespace Bronnoysund.Lookup.Infrastructure.Stubs;

// Placeholder implementations for registry ports that are not implemented in Phase 0.
// Throws RegistryNotAvailableException when called. Used as default registrations
// so the DI graph is complete. Replaced with real providers in Phase 4.

internal sealed class NotAvailableRolesProvider : IRolesProvider
{
    public Task<RolesResponse?> GetRolesAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("Roles", "Implemented in Phase 4. Requires no external access.");
}

internal sealed class NotAvailableAnnualReportProvider : IAnnualReportProvider
{
    public Task<AnnualReportResponse?> GetLatestAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("AnnualReport", "Requires Maskinporten access. Implemented in Phase 4+ once the agreement is in place.");
}

internal sealed class NotAvailableBeneficialOwnerProvider : IBeneficialOwnerProvider
{
    public Task<BeneficialOwnersResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("BeneficialOwners", "Restricted access (AML/authorities). Implemented in Phase 4+ once access is granted.");
}

internal sealed class NotAvailableDebtRegisterProvider : IDebtRegisterProvider
{
    public Task<DebtSummaryResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("DebtRegister", "Requires a commercial agreement. Not an open API.");
}

internal sealed class NotAvailableBankruptcyProvider : IBankruptcyProvider
{
    public Task<BankruptcyResponse?> GetAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("BankruptcyRegister", "Implemented in Phase 4. Open API.");
}

internal sealed class NotAvailableSubUnitsProvider : ISubUnitsProvider
{
    public Task<SubUnitsResponse?> GetSubUnitsAsync(OrganizationNumber org, CancellationToken ct) =>
        throw new RegistryNotAvailableException("SubUnits", "Implemented in Phase 4. Open API.");
}

internal sealed class NotAvailablePersonRolesProvider : IPersonRolesProvider
{
    public Task<PersonRolesResponse?> GetByPersonAsync(PersonIdentifier person, CancellationToken ct) =>
        throw new RegistryNotAvailableException("PersonRoles", "Implemented in Phase 4. Open API via reverse roles lookup.");
}
