// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Application.UseCases.SearchCompaniesByName;
using Bronnoysund.Domain;
using Bronnoysund.ViewModels.Resources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bronnoysund.ViewModels.Tests;

/// <summary>
/// Per N6: verify that LookupAsync drives the three enrichment ports and that Unavailable
/// results surface in EnrichmentErrors. Uses hand-rolled stubs to avoid pulling NSubstitute
/// into the ViewModels test project just for two tests.
/// </summary>
public sealed class CompanyLookupViewModelEnrichmentTests
{
    private const string Orgnr = "974760843";

    private static IStringLocalizer<SharedResources> NewLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResources>>();
    }

    [Fact]
    public async Task LookupAsync_PopulatesEnrichmentProperties()
    {
        var aggregator = new StubAggregator(new AggregatedCompanyResponse(
            Core: new CompanyResponse(Orgnr, "STATENS VEGVESEN", "ORGL", "Bokmål"),
            Roles: null, LatestAnnualReport: null, BeneficialOwners: null, Debt: null,
            Bankruptcy: null, SubUnits: null,
            Errors: []));
        var voluntary = new StubVoluntary(new VoluntaryOrganizationLookupResult.Found(
            new VoluntaryOrganizationResponse(
                OrganizationNumber: Orgnr,
                Status: "AKTIV",
                FirstRegisteredDate: new DateOnly(2010, 4, 15),
                RegisteredDate: new DateOnly(2024, 3, 1),
                PrimaryIcnpoCategoryNumber: "01100",
                PrimaryIcnpoCategoryName: "Kultur og kunst",
                ParticipatesInGrasrotandel: true,
                AccountNumber: null)));
        var legalRoles = new StubLegalRoles(new LegalRolesLookupResult.Found(
            new LegalRolesResponse(Orgnr, false, [
                new LegalRoleHolding("555666777", "TARGET AS",
                    [ new LegalRoleAssignment("DTPR", "Deltaker", false, false, 1) ])
            ])));
        var changes = new StubChanges(new EntityChangesResponse(
            Orgnr,
            new EntityChangesFeed([ new EntityChange(DateTimeOffset.UtcNow, "Endring", 1L) ]),
            new EntityChangesFeed([]),
            new EntityChangesFeed([])));

        var vm = NewViewModel(aggregator, voluntary, legalRoles, changes);
        vm.OrgNumberInput = Orgnr;

        await vm.LookupAsync(CancellationToken.None);

        vm.Found.Should().NotBeNull();
        vm.Found!.OrganizationName.Should().Be("STATENS VEGVESEN");
        vm.VoluntaryOrganization.Should().NotBeNull();
        vm.VoluntaryOrganization!.Status.Should().Be("AKTIV");
        vm.LegalRoles.Should().NotBeNull();
        vm.LegalRoles!.Holdings.Should().HaveCount(1);
        vm.Changes.Should().NotBeNull();
        vm.Changes!.EntityFeed.Changes.Should().HaveCount(1);
        vm.EnrichmentErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task LookupAsync_RecordsEnrichmentError_WhenVoluntaryUnavailable()
    {
        var aggregator = new StubAggregator(new AggregatedCompanyResponse(
            Core: new CompanyResponse(Orgnr, "TEST AS", "AS", "Bokmål"),
            Roles: null, LatestAnnualReport: null, BeneficialOwners: null, Debt: null,
            Bankruptcy: null, SubUnits: null,
            Errors: []));
        var voluntary = new StubVoluntary(new VoluntaryOrganizationLookupResult.Unavailable(
            "Frivillighetsregisteret is temporarily unavailable."));
        var legalRoles = new StubLegalRoles(new LegalRolesLookupResult.NotFound(Orgnr));
        var changes = new StubChanges(new EntityChangesResponse(
            Orgnr,
            new EntityChangesFeed([]),
            new EntityChangesFeed([]),
            new EntityChangesFeed([])));

        var vm = NewViewModel(aggregator, voluntary, legalRoles, changes);
        vm.OrgNumberInput = Orgnr;

        await vm.LookupAsync(CancellationToken.None);

        vm.VoluntaryOrganization.Should().BeNull();
        vm.EnrichmentErrors.Should().ContainSingle()
            .Which.RegistryName.Should().Be("Frivillighetsregisteret");
    }

    private static CompanyLookupViewModel NewViewModel(
        ICompanyDataAggregator aggregator,
        IVoluntaryOrganizationProvider voluntary,
        ILegalRolesProvider legalRoles,
        IEntityChangesProvider changes)
    {
        // Iter-3 follow-up (Plan 26 Steg 0): handler is now country-agnostic. For these
        // viewmodel-level tests we use a passthrough detector that converts the raw input
        // to an OrganizationNumber (the only path the existing enrichment tests exercise)
        // and a one-entry registry that routes "NO" to a provider backed by the same
        // aggregator stub so the existing test fixtures keep working.
        var detector = new PassthroughNorwegianDetector();
        var registry = new SingleNorwegianRegistry(new AggregatorBackedProvider(aggregator));
        var aggregatedHandler = new LookupAggregatedCompanyHandler(
            detector, registry, aggregator, new NoopHealthTracker(),
            NullLogger<LookupAggregatedCompanyHandler>.Instance);
        var searchHandler = new SearchCompaniesByNameHandler(
            new StubSearchProvider(), NullLogger<SearchCompaniesByNameHandler>.Instance);
        return new CompanyLookupViewModel(
            aggregatedHandler, searchHandler, NewLocalizer(),
            legalRoles, voluntary, changes);
    }

    private sealed class PassthroughNorwegianDetector : ICountryDetector
    {
        public CompanyIdentifier? Detect(string? rawInput) =>
            OrganizationNumber.TryCreate(rawInput, out var org, out _) ? org : null;
    }

    private sealed class SingleNorwegianRegistry(ICompanyProvider provider) : ICompanyProviderRegistry
    {
        public ICompanyProvider? GetForCountry(string countryCode) =>
            string.Equals(countryCode, "NO", StringComparison.OrdinalIgnoreCase) ? provider : null;
        public IReadOnlyCollection<string> SupportedCountries => ["NO"];
        public IReadOnlyDictionary<string, bool> ConfigurationStatus =>
            new Dictionary<string, bool> { ["NO"] = true };
    }

    private sealed class NoopHealthTracker : IProviderHealthTracker
    {
        public void RecordSuccess(string countryCode) { }
        public void RecordFailure(string countryCode, string? reason) { }
        public IReadOnlyDictionary<string, ProviderHealthSnapshot> Snapshot =>
            new Dictionary<string, ProviderHealthSnapshot>();
    }

    private sealed class AggregatorBackedProvider(ICompanyDataAggregator aggregator) : ICompanyProvider
    {
        public string CountryCode => "NO";
        public bool IsConfigured => true;
        public Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct) =>
            id is OrganizationNumber org
                ? aggregator.CoreOnlyAsync(org, ct)
                : Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.InvalidInput("not Norwegian"));
    }

    private sealed class StubAggregator(AggregatedCompanyResponse aggregated) : ICompanyDataAggregator
    {
        public Task<AggregatedCompanyResponse> AggregateAsync(OrganizationNumber org, AggregatedScope scope, CancellationToken ct)
            => Task.FromResult(aggregated);
        public Task<CompanyLookupResult> CoreOnlyAsync(OrganizationNumber org, CancellationToken ct)
            => Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.Found(aggregated.Core));
    }

    private sealed class StubVoluntary(VoluntaryOrganizationLookupResult result) : IVoluntaryOrganizationProvider
    {
        public Task<VoluntaryOrganizationLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class StubLegalRoles(LegalRolesLookupResult result) : ILegalRolesProvider
    {
        public Task<LegalRolesLookupResult> GetLegalRolesAsync(OrganizationNumber org, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class StubChanges(EntityChangesResponse result) : IEntityChangesProvider
    {
        public Task<EntityChangesResponse> GetChangesAsync(OrganizationNumber org, int pageSize, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class StubSearchProvider : ICompanySearchProvider
    {
        public Task<CompanySearchResult> SearchByNameAsync(string query, int maxResults, CancellationToken ct)
            => Task.FromResult(new CompanySearchResult([], 0));
        public Task<PagedResult<CompanySearchHit>> SearchByNameAsync(string query, PagedRequest paging, CancellationToken ct)
            => Task.FromResult(new PagedResult<CompanySearchHit>([], paging.Page, paging.PageSize, 0, 0));
    }
}
