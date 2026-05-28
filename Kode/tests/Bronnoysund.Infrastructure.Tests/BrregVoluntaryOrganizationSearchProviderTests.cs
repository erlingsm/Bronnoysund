// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Infrastructure.Brreg;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class BrregVoluntaryOrganizationSearchProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregVoluntaryOrganizationSearchProvider _sut;

    public BrregVoluntaryOrganizationSearchProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregVoluntaryOrganizationSearchProvider(brreg, NullLogger<BrregVoluntaryOrganizationSearchProvider>.Instance);
    }

    [Fact]
    public async Task SearchAsync_MapsHitsAndExtractsCursor()
    {
        const string body = """
            {
              "_embedded": {
                "frivilligeOrganisasjoner": [
                  {
                    "organisasjonsnummer": "974760843",
                    "frivilligOrganisasjonsstatus": "AKTIV",
                    "foersteGangInnfoert": "2010-04-15",
                    "innfoertDato": "2024-03-01",
                    "kontonummer": "12345678901",
                    "icnpoKategorier": [
                      { "icnpoNummer": "01100", "navn": "Kultur og kunst", "rekkefoelge": 1 }
                    ],
                    "grasrotandel": { "deltarI": true }
                  },
                  {
                    "organisasjonsnummer": "987654321",
                    "frivilligOrganisasjonsstatus": "AKTIV",
                    "innfoertDato": "2022-06-01",
                    "icnpoKategorier": [],
                    "grasrotandel": { "deltarI": false }
                  }
                ]
              },
              "_links": {
                "self": { "href": "https://data.brreg.no/frivillighetsregisteret/api/frivillige-organisasjoner?size=2" },
                "next": { "href": "https://data.brreg.no/frivillighetsregisteret/api/frivillige-organisasjoner?size=2&searchAfter=ABC%3D%3D" }
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(Size: 2), CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationSearchResult.Found>();
        var page = ((VoluntaryOrganizationSearchResult.Found)result).Result;
        page.Organizations.Should().HaveCount(2);
        page.Organizations[0].OrganizationNumber.Should().Be("974760843");
        page.Organizations[0].PrimaryIcnpoCategoryName.Should().Be("Kultur og kunst");
        page.Organizations[0].ParticipatesInGrasrotandel.Should().BeTrue();
        // M5: full field-mapping coverage — without these the mapper could silently
        // drop status / dates / account number on a Brreg field rename and the test
        // would still pass.
        page.Organizations[0].Status.Should().Be("AKTIV");
        page.Organizations[0].FirstRegisteredDate.Should().Be(new DateOnly(2010, 4, 15));
        page.Organizations[0].RegisteredDate.Should().Be(new DateOnly(2024, 3, 1));
        page.Organizations[0].PrimaryIcnpoCategoryNumber.Should().Be("01100");
        page.Organizations[0].AccountNumber.Should().Be("12345678901");
        page.Organizations[1].OrganizationNumber.Should().Be("987654321");
        page.NextCursor.Should().Be("ABC==");
    }

    [Fact]
    public async Task SearchAsync_PicksLowestRekkefoelgeAsPrimaryIcnpo()
    {
        // M6: when several ICNPO categories are returned, the primary should be the one with
        // the lowest rekkefoelge — not the first in the array. Verify by giving the lower
        // rekkefoelge to the second element so a naive FirstOrDefault would pick the wrong one.
        const string body = """
            {
              "_embedded": {
                "frivilligeOrganisasjoner": [
                  {
                    "organisasjonsnummer": "974760843",
                    "frivilligOrganisasjonsstatus": "AKTIV",
                    "icnpoKategorier": [
                      { "icnpoNummer": "03000", "navn": "Tertiary", "rekkefoelge": 3 },
                      { "icnpoNummer": "01100", "navn": "Primary", "rekkefoelge": 1 },
                      { "icnpoNummer": "02000", "navn": "Secondary", "rekkefoelge": 2 }
                    ]
                  }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(), CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationSearchResult.Found>();
        var page = ((VoluntaryOrganizationSearchResult.Found)result).Result;
        page.Organizations[0].PrimaryIcnpoCategoryNumber.Should().Be("01100");
        page.Organizations[0].PrimaryIcnpoCategoryName.Should().Be("Primary");
    }

    [Fact]
    public async Task SearchAsync_ReturnsNullCursor_WhenLinksHasNoNext()
    {
        // M2: explicit coverage for the no-next-link path. Today the empty-result test
        // covers this implicitly, but pinning it as its own test makes the contract
        // clear if the body shape later changes.
        const string body = """
            {
              "_embedded": {
                "frivilligeOrganisasjoner": [
                  { "organisasjonsnummer": "974760843" }
                ]
              },
              "_links": {
                "self": { "href": "https://data.brreg.no/frivillighetsregisteret/api/frivillige-organisasjoner?size=1" }
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(Size: 1), CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationSearchResult.Found>();
        var page = ((VoluntaryOrganizationSearchResult.Found)result).Result;
        page.Organizations.Should().HaveCount(1);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_ReturnsFoundWithEmptyList_OnEmptyResult()
    {
        const string body = """
            {
              "_embedded": { "frivilligeOrganisasjoner": [] },
              "_links": { "self": { "href": "..." } }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(), CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationSearchResult.Found>();
        var page = ((VoluntaryOrganizationSearchResult.Found)result).Result;
        page.Organizations.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_RejectsInvalidSize()
    {
        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(Size: 0), CancellationToken.None);
        result.Should().BeOfType<VoluntaryOrganizationSearchResult.InvalidInput>();

        var result2 = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(Size: 101), CancellationToken.None);
        result2.Should().BeOfType<VoluntaryOrganizationSearchResult.InvalidInput>();
    }

    [Fact]
    public async Task SearchAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(), CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationSearchResult.Unavailable>();
    }

    [Fact]
    public async Task SearchAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json")
                .WithBody("""{"_embedded":{"frivilligeOrganisasjoner":[]}}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.SearchAsync(new VoluntaryOrganizationSearchQuery(), cts.Token);

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
