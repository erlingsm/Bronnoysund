// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class BrregSubUnitDetailsProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregSubUnitDetailsProvider _sut;
    private static readonly OrganizationNumber RiksrevisjonenAvdBergen = OrganizationNumber.Create("923802029");

    public BrregSubUnitDetailsProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregSubUnitDetailsProvider(brreg, NullLogger<BrregSubUnitDetailsProvider>.Instance);
    }

    [Fact]
    public async Task LookupAsync_MapsCoreFields_OnLiveUnderenhet()
    {
        const string body = """
            {
              "respons_klasse": "Underenhet",
              "organisasjonsnummer": "923802029",
              "navn": "RIKSREVISJONEN AVD BERGEN",
              "overordnetEnhet": "974760843",
              "hjemmeside": "https://example.no",
              "epostadresse": "post@example.no",
              "telefon": "22123456",
              "beliggenhetsadresse": {
                "adresse": ["Storgata 1"],
                "postnummer": "5003",
                "poststed": "BERGEN",
                "kommune": "BERGEN",
                "kommunenummer": "4601",
                "land": "Norge",
                "landkode": "NO"
              },
              "postadresse": {
                "adresse": ["Postboks 123"],
                "postnummer": "5001",
                "poststed": "BERGEN",
                "kommune": "BERGEN",
                "kommunenummer": "4601",
                "land": "Norge",
                "landkode": "NO"
              },
              "naeringskode1": { "kode": "84.111", "beskrivelse": "Generell offentlig administrasjon" },
              "harRegistrertAntallAnsatte": true,
              "antallAnsatte": 42,
              "oppstartsdato": "2010-01-01",
              "registreringsdatoEnhetsregisteret": "2009-12-15",
              "registrertIMvaregisteret": false
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/underenheter/923802029").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(RiksrevisjonenAvdBergen, CancellationToken.None);

        result.Should().BeOfType<SubUnitLookupResult.Found>();
        var sub = ((SubUnitLookupResult.Found)result).SubUnit;
        sub.OrganizationNumber.Should().Be("923802029");
        sub.OrganizationName.Should().Be("RIKSREVISJONEN AVD BERGEN");
        sub.ParentOrganizationNumber.Should().Be("974760843");
        sub.Website.Should().Be("https://example.no");
        sub.Email.Should().Be("post@example.no");
        sub.Phone.Should().Be("22123456");
        sub.BusinessAddress.Should().NotBeNull();
        sub.BusinessAddress!.City.Should().Be("BERGEN");
        sub.BusinessAddress.StreetAddress.Should().Be("Storgata 1");
        sub.PostalAddress.Should().NotBeNull();
        sub.PostalAddress!.PostalCode.Should().Be("5001");
        sub.PrimaryIndustry.Should().NotBeNull();
        sub.PrimaryIndustry!.Code.Should().Be("84.111");
        sub.EmployeeCount.Should().Be(42);
        sub.StartDate.Should().Be(new DateOnly(2010, 1, 1));
        sub.RegisteredDate.Should().Be(new DateOnly(2009, 12, 15));
        sub.RegisteredInVatRegistry.Should().BeFalse();
    }

    [Fact]
    public async Task LookupAsync_ReturnsNotFound_OnSlettetUnderEnhet()
    {
        const string body = """
            {
              "respons_klasse": "SlettetUnderEnhet",
              "organisasjonsnummer": "923802029",
              "navn": "FJERNET ENHET"
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/underenheter/923802029").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(RiksrevisjonenAvdBergen, CancellationToken.None);

        result.Should().BeOfType<SubUnitLookupResult.NotFound>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsNotFound_OnHttp404()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/underenheter/923802029").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.LookupAsync(RiksrevisjonenAvdBergen, CancellationToken.None);

        result.Should().BeOfType<SubUnitLookupResult.NotFound>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/underenheter/923802029").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.LookupAsync(RiksrevisjonenAvdBergen, CancellationToken.None);

        result.Should().BeOfType<SubUnitLookupResult.Unavailable>();
    }

    [Fact]
    public async Task LookupAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/underenheter/923802029").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.LookupAsync(RiksrevisjonenAvdBergen, cts.Token);

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
