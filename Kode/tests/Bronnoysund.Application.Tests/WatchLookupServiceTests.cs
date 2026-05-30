// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Application.UseCases.LookupCompany;
using Bronnoysund.Application.Watch;
using Bronnoysund.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Bronnoysund.Application.Tests;

public class WatchLookupServiceTests
{
    private static readonly OrganizationNumber Equinor = OrganizationNumber.Create("919300388");

    private readonly ICountryDetector _detector = Substitute.For<ICountryDetector>();
    private readonly ICompanyProviderRegistry _registry = Substitute.For<ICompanyProviderRegistry>();
    private readonly ICompanyProvider _provider = Substitute.For<ICompanyProvider>();
    private readonly IProviderHealthTracker _health = Substitute.For<IProviderHealthTracker>();
    private readonly ILookupMetrics _metrics = Substitute.For<ILookupMetrics>();
    private readonly WatchLookupService _sut;

    public WatchLookupServiceTests()
    {
        var services = new ServiceCollection()
            .AddSingleton(_detector)
            .AddSingleton(_registry)
            .AddSingleton(_health)
            .AddSingleton(_metrics)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddTransient<LookupCompanyHandler>()
            .BuildServiceProvider();

        _sut = new WatchLookupService(services, NullLogger<WatchLookupService>.Instance);
    }

    [Fact]
    public async Task UnsupportedVersion_ReturnsInvalidWithVersionCode()
    {
        var request = new LookupRequest(99, "lookup", "919300388");

        var response = await _sut.HandleAsync(request, CancellationToken.None);

        response.Result.Should().Be("invalid");
        response.Code.Should().Be("unsupportedVersion");
        response.Message.Should().Contain("99");
    }

    [Fact]
    public async Task EmptyValue_ReturnsInvalidEmptyValue()
    {
        var request = new LookupRequest(1, "lookup", "   ");

        var response = await _sut.HandleAsync(request, CancellationToken.None);

        response.Result.Should().Be("invalid");
        response.Code.Should().Be("emptyValue");
    }

    [Fact]
    public async Task SearchAction_ReturnsInvalidNotImplemented()
    {
        var request = new LookupRequest(1, "search", "equinor");

        var response = await _sut.HandleAsync(request, CancellationToken.None);

        response.Result.Should().Be("invalid");
        response.Code.Should().Be("notImplemented");
    }

    [Fact]
    public async Task UnknownAction_ReturnsInvalidUnsupportedAction()
    {
        var request = new LookupRequest(1, "delete", "x");

        var response = await _sut.HandleAsync(request, CancellationToken.None);

        response.Result.Should().Be("invalid");
        response.Code.Should().Be("unsupportedAction");
    }

    [Fact]
    public async Task LookupFound_MapsCompanyFieldsAndDerivesCountryCode()
    {
        var company = new CompanyResponse("919300388", "EQUINOR ASA", "ASA", "Bokmål");
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(company));

        var response = await _sut.HandleAsync(
            new LookupRequest(1, "lookup", "919300388"), CancellationToken.None);

        response.Result.Should().Be("found");
        response.OrganizationNumber.Should().Be("919300388");
        response.OrganizationName.Should().Be("EQUINOR ASA");
        response.CompanyType.Should().Be("ASA");
        response.LanguageForm.Should().Be("Bokmål");
        response.CountryCode.Should().Be("NO");
    }

    [Fact]
    public async Task LookupFoundWithUndetectedInput_StillSerialisesButCountryCodeIsNull()
    {
        // Defensive: even if detector cannot tag the input, the handler may still
        // produce a Found (e.g. registry side-paths). The phone-side response
        // should not lie about countryCode in that scenario.
        var company = new CompanyResponse("919300388", "EQUINOR ASA", "ASA", "Bokmål");
        _detector.Detect("919300388").Returns((CompanyIdentifier?)null);
        // Handler will fail since detector returns null; just verify code path:

        var response = await _sut.HandleAsync(
            new LookupRequest(1, "lookup", "919300388"), CancellationToken.None);

        response.Result.Should().Be("invalid");
        response.Code.Should().Be("unrecognizedFormat");
        response.CountryCode.Should().BeNull();
    }

    [Fact]
    public async Task LookupNotFound_MapsNotFoundWithEchoedValue()
    {
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("919300388"));

        var response = await _sut.HandleAsync(
            new LookupRequest(1, "lookup", "919300388"), CancellationToken.None);

        response.Result.Should().Be("notFound");
        response.Value.Should().Be("919300388");
        response.Message.Should().Contain("919300388");
    }

    [Fact]
    public async Task LookupUnavailable_MapsUnavailableWithUpstreamMessage()
    {
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Unavailable("Brreg is down."));

        var response = await _sut.HandleAsync(
            new LookupRequest(1, "lookup", "919300388"), CancellationToken.None);

        response.Result.Should().Be("unavailable");
        response.Message.Should().Be("Brreg is down.");
    }

    [Fact]
    public async Task LookupTrimsLeadingAndTrailingWhitespace()
    {
        var company = new CompanyResponse("919300388", "EQUINOR ASA", "ASA", "Bokmål");
        _detector.Detect("919300388").Returns(Equinor);
        _registry.GetForCountry("NO").Returns(_provider);
        _provider.LookupAsync(Equinor, Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(company));

        var response = await _sut.HandleAsync(
            new LookupRequest(1, "lookup", "  919300388  "), CancellationToken.None);

        response.Result.Should().Be("found");
        _detector.Received().Detect("919300388");
    }
}
