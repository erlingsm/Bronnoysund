// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Bronnoysund.Application.Tests;

public class LookupCompanyHandlerTests
{
    private readonly ICountryDetector _detector = Substitute.For<ICountryDetector>();
    private readonly ICompanyProviderRegistry _registry = Substitute.For<ICompanyProviderRegistry>();
    private readonly ICompanyProvider _provider = Substitute.For<ICompanyProvider>();
    private readonly IProviderHealthTracker _health = Substitute.For<IProviderHealthTracker>();
    private readonly LookupCompanyHandler _sut;
    private static readonly OrganizationNumber Equinor = OrganizationNumber.Create("919300388");

    public LookupCompanyHandlerTests()
    {
        _sut = new LookupCompanyHandler(_detector, _registry, _health, NullLogger<LookupCompanyHandler>.Instance);
    }

    [Fact]
    public async Task ValidOrgNumber_DispatchesToRegistryProvider_AndReturnsFound()
    {
        var expected = new CompanyResponse("919300388", "Equinor ASA", "AS", "Bokmål");
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(expected));

        var result = await _sut.HandleAsync(new LookupCompanyQuery("919300388"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Found>()
            .Which.Company.Should().Be(expected);
    }

    [Fact]
    public async Task UndetectedInput_ReturnsInvalidInput_WithoutHittingRegistry()
    {
        _detector.Detect("12345").Returns((CompanyIdentifier?)null);

        var result = await _sut.HandleAsync(new LookupCompanyQuery("12345"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.InvalidInput>();
        _registry.DidNotReceiveWithAnyArgs().GetForCountry(default!);
    }

    [Fact]
    public async Task EmptyInput_ReturnsInvalidInput()
    {
        _detector.Detect("").Returns((CompanyIdentifier?)null);

        var result = await _sut.HandleAsync(new LookupCompanyQuery(""), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.InvalidInput>();
    }

    [Fact]
    public async Task DetectedButNoProviderRegistered_ReturnsUnavailable()
    {
        _detector.Detect("ZZ123").Returns(Equinor); // pretend detector lies; registry decides
        _registry.GetForCountry("NO").Returns((ICompanyProvider?)null);

        var result = await _sut.HandleAsync(new LookupCompanyQuery("ZZ123"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("NO");
    }

    [Fact]
    public async Task ProviderReturnsNotFound_PropagatesNotFound()
    {
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("919300388"));

        var result = await _sut.HandleAsync(new LookupCompanyQuery("919300388"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.NotFound>()
            .Which.OrganizationNumber.Should().Be("919300388");
    }

    [Fact]
    public async Task ProviderReturnsUnavailable_PropagatesUnavailable()
    {
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Unavailable("Brreg is down."));

        var result = await _sut.HandleAsync(new LookupCompanyQuery("919300388"), CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Be("Brreg is down.");
    }
}
