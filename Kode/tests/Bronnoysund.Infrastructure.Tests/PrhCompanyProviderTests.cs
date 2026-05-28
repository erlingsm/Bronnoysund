// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Finland.Prh;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// End-to-end through WireMock against the Kiota-generated PrhClient: exercises the
/// /companies?businessId= search shape, the 0-results=NotFound branch, error translation,
/// and full mapping of a representative payload. A PRH-side schema change shows up here
/// before any caller code notices.
/// </summary>
public class PrhCompanyProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly PrhCompanyProvider _sut;
    private static readonly FinnishBusinessId Nokia = FinnishBusinessId.Create("0112038-9");

    public PrhCompanyProviderTests()
    {
        _wireMock = WireMockServer.Start();
        var client = TestPrhClientFactory.ForWireMock(_wireMock);
        _sut = new PrhCompanyProvider(client, NullLogger<PrhCompanyProvider>.Instance);
    }

    [Fact]
    public async Task Lookup_HappyPath_MapsAllRepresentativeFields()
    {
        const string body = """
            {
              "totalResults": 1,
              "companies": [
                {
                  "businessId": { "value": "0112038-9", "registrationDate": "1967-04-30", "source": "1" },
                  "euId": { "value": "FIPRO.0112038-9", "source": "1" },
                  "names": [
                    { "name": "Nokia Oyj", "type": "1", "version": 1, "source": "1", "registrationDate": "1995-05-19" }
                  ],
                  "mainBusinessLine": {
                    "type": "61200",
                    "typeCodeSet": "TOIMI3",
                    "source": "1",
                    "registrationDate": "2008-01-01",
                    "descriptions": [
                      { "languageCode": "1", "description": "Langaton viestintä" },
                      { "languageCode": "2", "description": "Trådlös kommunikation" },
                      { "languageCode": "3", "description": "Wireless telecommunications" }
                    ]
                  },
                  "registrationDate": "1967-04-30",
                  "endDate": null,
                  "lastModified": "2026-04-12T11:22:33+03:00",
                  "status": "2",
                  "tradeRegisterStatus": "1",
                  "website": { "url": "https://www.nokia.com", "source": "1" },
                  "companyForms": [
                    {
                      "type": "OYJ",
                      "version": 2,
                      "source": "1",
                      "registrationDate": "2000-12-12",
                      "endDate": null,
                      "descriptions": [
                        { "languageCode": "3", "description": "Public limited company" }
                      ]
                    }
                  ],
                  "companySituations": [],
                  "addresses": [
                    {
                      "type": 1,
                      "street": "Karaportti",
                      "buildingNumber": "3",
                      "postCode": "02610",
                      "country": "FI",
                      "source": "1",
                      "registrationDate": "2010-01-01",
                      "postOffices": [
                        { "languageCode": "1", "city": "ESPOO", "municipalityCode": "049" },
                        { "languageCode": "2", "city": "ESBO", "municipalityCode": "049" }
                      ]
                    },
                    {
                      "type": 2,
                      "street": "Karaportti",
                      "buildingNumber": "3",
                      "postCode": "02610",
                      "country": "FI",
                      "source": "1",
                      "registrationDate": "2010-01-01",
                      "postOffices": [
                        { "languageCode": "1", "city": "ESPOO", "municipalityCode": "049" }
                      ]
                    }
                  ],
                  "registeredEntries": []
                }
              ]
            }
            """;
        StubCompaniesEndpoint(body);

        var result = await _sut.LookupAsync(Nokia, CancellationToken.None);

        var company = result.Should().BeOfType<CompanyLookupResult.Found>().Subject.Company;
        company.OrganizationNumber.Should().Be("0112038-9");
        company.OrganizationName.Should().Be("Nokia Oyj");
        company.CompanyType.Should().Be("OYJ");
        company.LanguageForm.Should().Be("Unknown"); // PRH has no målform concept
        company.Website.Should().Be("https://www.nokia.com");
        company.PrimaryIndustry.Should().NotBeNull();
        company.PrimaryIndustry!.Code.Should().Be("61200");
        company.PrimaryIndustry.Description.Should().Be("Wireless telecommunications");
        company.BusinessAddress.Should().NotBeNull();
        company.BusinessAddress!.StreetAddress.Should().Be("Karaportti 3");
        company.BusinessAddress.PostalCode.Should().Be("02610");
        company.BusinessAddress.City.Should().Be("ESPOO");
        company.BusinessAddress.Country.Should().Be("FI");
        company.PostalAddress.Should().NotBeNull();
        company.RegisteredDate.Should().Be(new DateOnly(1967, 4, 30));
        company.IsBankrupt.Should().BeFalse();
    }

    [Fact]
    public async Task Lookup_ZeroResults_ReturnsNotFound()
    {
        StubCompaniesEndpoint("""{ "totalResults": 0, "companies": [] }""");

        var result = await _sut.LookupAsync(Nokia, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.NotFound>()
            .Which.OrganizationNumber.Should().Be("0112038-9");
    }

    [Fact]
    public async Task Lookup_BankruptcySituation_SurfacesAsIsBankruptTrue()
    {
        const string body = """
            {
              "totalResults": 1,
              "companies": [
                {
                  "businessId": { "value": "0112038-9", "source": "1" },
                  "names": [ { "name": "Konkursi Oy", "type": "1", "source": "1" } ],
                  "addresses": [],
                  "companyForms": [],
                  "companySituations": [
                    { "type": "KONK", "source": "1", "registrationDate": "2024-08-15", "endDate": null }
                  ],
                  "registeredEntries": []
                }
              ]
            }
            """;
        StubCompaniesEndpoint(body);

        var result = await _sut.LookupAsync(Nokia, CancellationToken.None);

        var company = result.Should().BeOfType<CompanyLookupResult.Found>().Subject.Company;
        company.IsBankrupt.Should().BeTrue();
        company.BankruptcyDate.Should().Be(new DateOnly(2024, 8, 15));
    }

    [Fact]
    public async Task Lookup_PartialPayload_StillMapsWhatItHas()
    {
        // A response that only contains the minimum required fields. PRH may strip optional
        // fields in the future; the mapper must not throw.
        const string body = """
            {
              "totalResults": 1,
              "companies": [
                {
                  "businessId": { "value": "0112038-9", "source": "1" },
                  "names": [ { "name": "Minimal Oy", "type": "1", "source": "1" } ]
                }
              ]
            }
            """;
        StubCompaniesEndpoint(body);

        var result = await _sut.LookupAsync(Nokia, CancellationToken.None);

        var company = result.Should().BeOfType<CompanyLookupResult.Found>().Subject.Company;
        company.OrganizationName.Should().Be("Minimal Oy");
        company.CompanyType.Should().Be("UKJENT");
        company.BusinessAddress.Should().BeNull();
        company.PrimaryIndustry.Should().BeNull();
        company.Website.Should().BeNull();
    }

    [Fact]
    public async Task Lookup_ServerError_ReturnsUnavailable()
    {
        _wireMock.Given(Request.Create().WithPath("/companies").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("upstream broken"));

        var result = await _sut.LookupAsync(Nokia, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.Unavailable>()
            .Which.Message.Should().Contain("500");
    }

    [Fact]
    public async Task Lookup_NorwegianIdentifier_ReturnsInvalidInput()
    {
        var norwegian = OrganizationNumber.Create("974760843");

        var result = await _sut.LookupAsync(norwegian, CancellationToken.None);

        result.Should().BeOfType<CompanyLookupResult.InvalidInput>()
            .Which.Message.Should().Contain("Finnish");
    }

    [Fact]
    public void CountryCode_IsFI()
    {
        _sut.CountryCode.Should().Be("FI");
    }

    private void StubCompaniesEndpoint(string body) =>
        _wireMock.Given(Request.Create().WithPath("/companies").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

    public void Dispose()
    {
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
