// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class BrregKodeverkProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregKodeverkProvider _sut;

    public BrregKodeverkProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregKodeverkProvider(brreg, NullLogger<BrregKodeverkProvider>.Instance);
    }

    [Fact]
    public async Task GetOrganisasjonsformerAsync_MapsCodeAndDescriptionAndRetiredFlag()
    {
        const string body = """
            {
              "_embedded": {
                "organisasjonsformer": [
                  { "kode": "AS", "beskrivelse": "Aksjeselskap" },
                  { "kode": "ENK", "beskrivelse": "Enkeltpersonforetak" },
                  { "kode": "OBS", "beskrivelse": "Obsolete form", "utgaatt": "1995-01-01" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetOrganisasjonsformerAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(3);
        entries[0].Code.Should().Be("AS");
        entries[0].Description.Should().Be("Aksjeselskap");
        entries[0].IsRetired.Should().BeFalse();
        entries[2].IsRetired.Should().BeTrue();
    }

    [Fact]
    public async Task GetIcnpoCategoriesAsync_MapsNumberAndName()
    {
        const string body = """
            {
              "_embedded": {
                "icnpoKategorier": [
                  { "icnpoNummer": "01100", "navn": "Kultur og kunst", "spraakkode": "NOB" },
                  { "icnpoNummer": "01200", "navn": "Idrett", "spraakkode": "NOB" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/icnpo-kategorier").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.GetIcnpoCategoriesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(2);
        entries[0].Code.Should().Be("01100");
        entries[0].Description.Should().Be("Kultur og kunst");
        entries[1].Code.Should().Be("01200");
    }

    [Fact]
    public async Task GetIcnpoCategoriesAsync_SkipsEntriesWithoutCode()
    {
        const string body = """
            {
              "_embedded": {
                "icnpoKategorier": [
                  { "icnpoNummer": "01100", "navn": "Kultur" },
                  { "navn": "Uten kode" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/icnpo-kategorier").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.GetIcnpoCategoriesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(1);
        entries[0].Code.Should().Be("01100");
    }

    [Fact]
    public async Task GetVoluntaryInformationTypesAsync_MapsIdentifierAndName()
    {
        const string body = """
            {
              "_embedded": {
                "informasjonstyper": [
                  { "identifikator": "VEDTEKTER", "navn": "Vedtekter", "spraakkode": "NOB" },
                  { "identifikator": "ICNPO_KATEGORI", "navn": "ICNPO-kategori", "spraakkode": "NOB" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/informasjonstyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.GetVoluntaryInformationTypesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(2);
        entries[0].Code.Should().Be("VEDTEKTER");
        entries[0].Description.Should().Be("Vedtekter");
        entries[1].Code.Should().Be("ICNPO_KATEGORI");
    }

    [Fact]
    public async Task GetVoluntaryInformationTypesAsync_SkipsEntriesWithoutIdentifier()
    {
        // M4: mirror the corresponding ICNPO skip-test so the Where(...)-filter has
        // symmetric coverage across all three kodeverk methods.
        const string body = """
            {
              "_embedded": {
                "informasjonstyper": [
                  { "identifikator": "VEDTEKTER", "navn": "Vedtekter" },
                  { "navn": "Uten identifikator" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/informasjonstyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.GetVoluntaryInformationTypesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(1);
        entries[0].Code.Should().Be("VEDTEKTER");
    }

    [Fact]
    public async Task GetIcnpoCategoriesAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/icnpo-kategorier").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetIcnpoCategoriesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Unavailable>();
    }

    [Fact]
    public async Task GetVoluntaryInformationTypesAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/informasjonstyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetVoluntaryInformationTypesAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Unavailable>();
    }

    [Fact]
    public async Task GetIcnpoCategoriesAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        // M3: a pre-cancelled CTS must surface as OperationCanceledException — Result-pattern
        // is only for transient unavailability, not for client-driven cancellation.
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/icnpo-kategorier").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json")
                .WithBody("""{"_embedded":{"icnpoKategorier":[]}}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetIcnpoCategoriesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetVoluntaryInformationTypesAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        // M3: mirror the ICNPO cancellation test for the third kodeverk method.
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/informasjonstyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json")
                .WithBody("""{"_embedded":{"informasjonstyper":[]}}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetVoluntaryInformationTypesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
