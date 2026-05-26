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

        var result = await sut.AggregateAsync(Vegvesenet, CancellationToken.None);

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

        var result = await sut.AggregateAsync(Vegvesenet, CancellationToken.None);

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

        var result = await sut.AggregateAsync(Vegvesenet, CancellationToken.None);

        result.LatestAnnualReport.Should().BeNull();
        result.Errors.Should().NotContain(e => e.RegistryName == "annualReport");
    }

    [Fact]
    public async Task AggregateAsync_CoreNotFound_Throws()
    {
        var sut = NewSut(out var providers);
        providers.Company.LookupAsync(Vegvesenet, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("974760843"));

        var act = async () => await sut.AggregateAsync(Vegvesenet, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-existent*974760843*");
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
            Substitute.For<ISubUnitsProvider>());

        // Default: optional providers return null (no data, no error).
        providers.Roles.GetRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((RolesResponse?)null);
        providers.AnnualReport.GetLatestAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((AnnualReportResponse?)null);
        providers.BeneficialOwner.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((BeneficialOwnersResponse?)null);
        providers.Debt.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((DebtSummaryResponse?)null);
        providers.Bankruptcy.GetAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((BankruptcyResponse?)null);
        providers.SubUnits.GetSubUnitsAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>()).Returns((SubUnitsResponse?)null);

        return new ParallelCompanyDataAggregator(
            providers.Company, providers.Roles, providers.AnnualReport, providers.BeneficialOwner,
            providers.Debt, providers.Bankruptcy, providers.SubUnits,
            NullLogger<ParallelCompanyDataAggregator>.Instance);
    }

    private sealed record Providers(
        ICompanyProvider Company,
        IRolesProvider Roles,
        IAnnualReportProvider AnnualReport,
        IBeneficialOwnerProvider BeneficialOwner,
        IDebtRegisterProvider Debt,
        IBankruptcyProvider Bankruptcy,
        ISubUnitsProvider SubUnits);
}
