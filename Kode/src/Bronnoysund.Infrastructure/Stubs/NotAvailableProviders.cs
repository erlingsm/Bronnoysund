// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Exceptions;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Stubs;

// SUMMARY — two stub patterns side-by-side
// ---------------------------------------------------------------------------
// The file mixes two unavailability conventions, intentionally. New code
// should prefer the "Result-type"-pattern; the throw-based one survives only
// because the surrounding aggregator already knows how to demote it.
//
// Old stubs (throw RegistryNotAvailableException):
//   - IRolesProvider, IAnnualReportProvider, IBeneficialOwnerProvider,
//     IDebtRegisterProvider, IBankruptcyProvider, ISubUnitsProvider,
//     IPersonRolesProvider.
//   These are consumed via ParallelCompanyDataAggregator which catches
//   RegistryNotAvailableException specifically and demotes the result to null
//   (with a RegistryError entry). Direct callers would see the exception
//   bubble up — but in practice only the aggregator wraps these ports.
//
// New stubs (return Result-types with an Unavailable variant):
//   - ISubUnitDetailsProvider, ILegalRolesProvider, IEntityChangesProvider,
//     IVoluntaryOrganizationProvider.
//   These are consumed directly by the WebApi endpoints and the
//   CompanyLookupViewModel, both of which handle Result discriminated unions
//   explicitly. No exception-translation is needed at the caller.
//
// TODO (Plan B-iterasjon): converting the old stubs to the Result-pattern is
// a natural cut-point during Plan B — when an old port gets a real adapter,
// switch the port + its stub to the Result-pattern in the same PR rather
// than leaving the discrepancy lingering.
// ---------------------------------------------------------------------------
//
// Placeholder implementations for registry ports that are not implemented in Phase 0.
// Used as default registrations so the DI graph is complete. Replaced with real
// providers in Phase 4.

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

internal sealed class NotAvailableSubUnitDetailsProvider : ISubUnitDetailsProvider
{
    public Task<SubUnitLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct) =>
        Task.FromResult<SubUnitLookupResult>(new SubUnitLookupResult.Unavailable(
            "SubUnitDetails is only available in Direct mode. Switch DataSource:Mode=Direct."));
}

internal sealed class NotAvailableLegalRolesProvider : ILegalRolesProvider
{
    public Task<LegalRolesLookupResult> GetLegalRolesAsync(OrganizationNumber org, CancellationToken ct) =>
        Task.FromResult<LegalRolesLookupResult>(new LegalRolesLookupResult.Unavailable(
            "LegalRoles is only available in Direct mode. Switch DataSource:Mode=Direct."));
}

internal sealed class NotAvailableEntityChangesProvider : IEntityChangesProvider
{
    public Task<EntityChangesResponse> GetChangesAsync(OrganizationNumber org, int pageSize, CancellationToken ct)
    {
        const string Message = "EntityChanges is only available in Direct mode. Switch DataSource:Mode=Direct.";
        return Task.FromResult(new EntityChangesResponse(
            OrganizationNumber: org.Value,
            EntityFeed: new EntityChangesFeed([], Message),
            SubUnitFeed: new EntityChangesFeed([], Message),
            RoleFeed: new EntityChangesFeed([], Message)));
    }
}

internal sealed class NotAvailableVoluntaryOrganizationProvider : IVoluntaryOrganizationProvider
{
    public Task<VoluntaryOrganizationLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct) =>
        Task.FromResult<VoluntaryOrganizationLookupResult>(new VoluntaryOrganizationLookupResult.Unavailable(
            "Frivillighetsregisteret is only available in Direct mode. Switch DataSource:Mode=Direct."));
}

internal sealed class NotAvailableVoluntaryOrganizationSearchProvider : IVoluntaryOrganizationSearchProvider
{
    public Task<VoluntaryOrganizationSearchResult> SearchAsync(VoluntaryOrganizationSearchQuery query, CancellationToken ct) =>
        Task.FromResult<VoluntaryOrganizationSearchResult>(new VoluntaryOrganizationSearchResult.Unavailable(
            "Frivillighetsregisteret search is only available in Direct mode. Switch DataSource:Mode=Direct."));
}

internal sealed class NotAvailableKodeverkProvider : IKodeverkProvider
{
    private const string Reason = " is only available in Direct mode. Switch DataSource:Mode=Direct.";

    private static Task<KodeverkLookupResult> NotAvailableList(string endpoint) =>
        Task.FromResult<KodeverkLookupResult>(new KodeverkLookupResult.Unavailable($"Kodeverk {endpoint}{Reason}"));

    private static Task<KodeverkSingleResult> NotAvailableSingle(string endpoint) =>
        Task.FromResult<KodeverkSingleResult>(new KodeverkSingleResult.Unavailable($"Kodeverk {endpoint}{Reason}"));

    private static Task<KodeverkPagedResult> NotAvailablePaged(string endpoint) =>
        Task.FromResult<KodeverkPagedResult>(new KodeverkPagedResult.Unavailable($"Kodeverk {endpoint}{Reason}"));

    public Task<KodeverkLookupResult> GetOrganisasjonsformerAsync(CancellationToken ct) =>
        NotAvailableList("organisasjonsformer");

    public Task<KodeverkLookupResult> GetIcnpoCategoriesAsync(CancellationToken ct) =>
        NotAvailableList("icnpo-kategorier");

    public Task<KodeverkLookupResult> GetVoluntaryInformationTypesAsync(CancellationToken ct) =>
        NotAvailableList("informasjonstyper");

    public Task<KodeverkPagedResult> GetKommunerAsync(int page, int size, CancellationToken ct) =>
        NotAvailablePaged("kommuner");

    public Task<KodeverkSingleResult> GetKommuneAsync(string kommunenummer, CancellationToken ct) =>
        NotAvailableSingle("kommuner");

    public Task<KodeverkLookupResult> GetRolletyperAsync(CancellationToken ct) =>
        NotAvailableList("rolletyper");

    public Task<KodeverkLookupResult> GetRollegruppetyperAsync(CancellationToken ct) =>
        NotAvailableList("rollegruppetyper");

    public Task<KodeverkLookupResult> GetRepresentanterAsync(CancellationToken ct) =>
        NotAvailableList("representanter");

    public Task<KodeverkSingleResult> GetOrganisasjonsformAsync(string kode, CancellationToken ct) =>
        NotAvailableSingle("organisasjonsformer");

    public Task<KodeverkLookupResult> GetOrganisasjonsformerWithEnheterAsync(CancellationToken ct) =>
        NotAvailableList("organisasjonsformer/enheter");

    public Task<KodeverkLookupResult> GetOrganisasjonsformerWithUnderenheterAsync(CancellationToken ct) =>
        NotAvailableList("organisasjonsformer/underenheter");
}

internal sealed class NotAvailableBrregStatisticsProvider : IBrregStatisticsProvider
{
    public Task<RolesTotalCountResult> GetRolesTotalCountAsync(CancellationToken ct) =>
        Task.FromResult<RolesTotalCountResult>(new RolesTotalCountResult.Unavailable(
            "Brreg statistics is only available in Direct mode. Switch DataSource:Mode=Direct."));
}

internal sealed class NotAvailablePersonRolesProvider : IPersonRolesProvider
{
    public Task<PersonRolesResponse?> GetByPersonAsync(PersonIdentifier person, CancellationToken ct) =>
        throw new RegistryNotAvailableException("PersonRoles", "Implemented in Phase 4. Open API via reverse roles lookup.");
}
