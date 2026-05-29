// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Exceptions;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Aggregation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class ParallelCompanyDataAggregatorTests
{
    private static readonly OrganizationNumber Vegvesenet = OrganizationNumber.Create("974760843");
    private static readonly CompanyResponse CoreFound = new("974760843", "RIKSREVISJONEN", "ORGL", "Bokmål");

    [Fact]
    public async Task AggregateAsync_AllProvidersReturnData_FullResponseNoErrors()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));
        providers.Roles.GetRolesAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new RolesResponse([new Role("Boss Person", null, "Daglig leder")]));
        providers.SubUnits.GetSubUnitsAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new SubUnitsResponse([new SubUnit("974707314", "RIKSREVISJONEN")]));
        providers.Bankruptcy.GetAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new BankruptcyResponse(false, null));

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.CoreOnly, CancellationToken.None);

        result.Core.Should().Be(CoreFound);
        result.Roles!.Roles.Should().HaveCount(1);
        result.SubUnits!.SubUnits.Should().HaveCount(1);
        result.Bankruptcy!.IsBankrupt.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_OneProviderThrows_OtherStillSucceed_ErrorRecorded()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));
        providers.Roles.GetRolesAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns<Task<RolesResponse?>>(_ => throw new InvalidOperationException("roles broke"));
        providers.SubUnits.GetSubUnitsAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new SubUnitsResponse([]));

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.CoreOnly, CancellationToken.None);

        result.Core.Should().Be(CoreFound);
        result.Roles.Should().BeNull();
        result.SubUnits.Should().NotBeNull();
        result.Errors.Should().ContainSingle(e => e.RegistryName == "roles" && e.Message.Contains("roles broke"));
    }

    [Fact]
    public async Task AggregateAsync_StubProviderThrowsRegistryNotAvailable_NoErrorLogged()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));
        providers.AnnualReport.GetLatestAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns<Task<AnnualReportResponse?>>(_ =>
                throw new RegistryNotAvailableException("AnnualReport", "Requires Maskinporten"));

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.CoreOnly, CancellationToken.None);

        result.LatestAnnualReport.Should().BeNull();
        result.Errors.Should().NotContain(e => e.RegistryName == "annualReport");
    }

    [Fact]
    public async Task AggregateAsync_CoreNotFound_Throws()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("974760843"));

        var act = async () => await sut.AggregateAsync(Vegvesenet, AggregatedScope.CoreOnly, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-existent*974760843*");
    }

    [Fact]
    public async Task AggregateAsync_CoreOnlyScope_DoesNotCallEnrichmentProviders()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.CoreOnly, CancellationToken.None);

        result.SubUnitDetails.Should().BeNull();
        result.LegalRoles.Should().BeNull();
        result.Voluntary.Should().BeNull();
        result.Changes.Should().BeNull();
        await providers.SubUnitDetails.DidNotReceive().LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
        await providers.LegalRoles.DidNotReceive().GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
        await providers.Voluntary.DidNotReceive().LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
        await providers.EntityChanges.DidNotReceive().GetChangesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_IncludeEnrichmentScope_CallsAllFourEnrichmentProviders()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));

        var subUnit = new SubUnitDetailsResponse("974760843", "RIKSREVISJONEN", "974760843");
        var legalRoles = new LegalRolesResponse("974760843", IsDeleted: false, Holdings: []);
        var voluntary = new VoluntaryOrganizationResponse(
            "974760843", "REGISTRERT", FirstRegisteredDate: null, RegisteredDate: null,
            PrimaryIcnpoCategoryNumber: null, PrimaryIcnpoCategoryName: null,
            ParticipatesInGrasrotandel: false, AccountNumber: null);
        var changes = new EntityChangesResponse(
            "974760843",
            new EntityChangesFeed([]),
            new EntityChangesFeed([]),
            new EntityChangesFeed([]));

        providers.SubUnitDetails.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.Found(subUnit));
        providers.LegalRoles.GetLegalRolesAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.Found(legalRoles));
        providers.Voluntary.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.Found(voluntary));
        providers.EntityChanges.GetChangesAsync(Vegvesenet, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(changes);

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.IncludeEnrichment, CancellationToken.None);

        result.SubUnitDetails.Should().Be(subUnit);
        result.LegalRoles.Should().Be(legalRoles);
        result.Voluntary.Should().Be(voluntary);
        result.Changes.Should().Be(changes);
        result.Errors.Should().BeEmpty();
        await providers.SubUnitDetails.Received(1).LookupAsync(Vegvesenet, Arg.Any<CancellationToken>());
        await providers.LegalRoles.Received(1).GetLegalRolesAsync(Vegvesenet, Arg.Any<CancellationToken>());
        await providers.Voluntary.Received(1).LookupAsync(Vegvesenet, Arg.Any<CancellationToken>());
        await providers.EntityChanges.Received(1).GetChangesAsync(Vegvesenet, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_IncludeEnrichment_UnavailableEnrichmentProviderDemotedToRegistryError()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(CoreFound));
        providers.Voluntary.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.Unavailable("Frivillighetsregisteret nede"));
        providers.LegalRoles.GetLegalRolesAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.NotFound("974760843"));

        var result = await sut.AggregateAsync(Vegvesenet, AggregatedScope.IncludeEnrichment, CancellationToken.None);

        result.Voluntary.Should().BeNull();
        result.LegalRoles.Should().BeNull();
        result.Errors.Should().ContainSingle(e => e.RegistryName == "voluntary"
            && e.Message.Contains("Frivillighetsregisteret nede"));
        // NotFound is a meaningful business answer, NOT an error.
        result.Errors.Should().NotContain(e => e.RegistryName == "legalRoles");
    }

    private static ParallelCompanyDataAggregator NewSut(out Providers providers)
    {
        providers = new Providers(
            Substitute.For<ICompanyProvider>(),
            Substitute.For<IRolesProvider>(),
            Substitute.For<IAnnualReportProvider>(),
            Substitute.For<IBeneficialOwnerProvider>(),
            Substitute.For<IDebtRegisterProvider>(),
            Substitute.For<IBankruptcyProvider>(),
            Substitute.For<ISubUnitsProvider>(),
            Substitute.For<ISubUnitDetailsProvider>(),
            Substitute.For<ILegalRolesProvider>(),
            Substitute.For<IVoluntaryOrganizationProvider>(),
            Substitute.For<IEntityChangesProvider>());

        // Default: optional providers return null (no data, no error).
        providers.Roles.GetRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((RolesResponse?)null);
        providers.AnnualReport.GetLatestAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((AnnualReportResponse?)null);
        providers.BeneficialOwner.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((BeneficialOwnersResponse?)null);
        providers.Debt.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((DebtSummaryResponse?)null);
        providers.Bankruptcy.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((BankruptcyResponse?)null);
        providers.SubUnits.GetSubUnitsAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((SubUnitsResponse?)null);

        return new ParallelCompanyDataAggregator(
            new SingleNorwegianRegistry(providers.Company), providers.Roles, providers.AnnualReport, providers.BeneficialOwner,
            providers.Debt, providers.Bankruptcy, providers.SubUnits,
            providers.SubUnitDetails, providers.LegalRoles, providers.Voluntary, providers.EntityChanges,
            NullLogger<ParallelCompanyDataAggregator>.Instance);
    }

    // The aggregator now resolves its Norwegian provider through ICompanyProviderRegistry so
    // the integration test wires the mock company under that registry shape. Mirrors the
    // SingleNorwegianRegistry helper used in the ViewModels-level enrichment tests.
    private sealed class SingleNorwegianRegistry(ICompanyProvider provider) : ICompanyProviderRegistry
    {
        public ICompanyProvider? GetForCountry(string countryCode) =>
            string.Equals(countryCode, "NO", StringComparison.OrdinalIgnoreCase) ? provider : null;
        public IReadOnlyCollection<string> SupportedCountries => ["NO"];
        public IReadOnlyDictionary<string, bool> ConfigurationStatus =>
            new Dictionary<string, bool> { ["NO"] = true };
    }

    private sealed record Providers(
        ICompanyProvider Company,
        IRolesProvider Roles,
        IAnnualReportProvider AnnualReport,
        IBeneficialOwnerProvider BeneficialOwner,
        IDebtRegisterProvider Debt,
        IBankruptcyProvider Bankruptcy,
        ISubUnitsProvider SubUnits,
        ISubUnitDetailsProvider SubUnitDetails,
        ILegalRolesProvider LegalRoles,
        IVoluntaryOrganizationProvider Voluntary,
        IEntityChangesProvider EntityChanges);
}
