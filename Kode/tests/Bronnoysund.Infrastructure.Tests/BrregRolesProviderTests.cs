// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Brreg;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class BrregRolesProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregRolesProvider _sut;
    private static readonly OrganizationNumber Vegvesenet = OrganizationNumber.Create("974760843");

    public BrregRolesProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregRolesProvider(brreg, NullLogger<BrregRolesProvider>.Instance);
    }

    [Fact]
    public async Task GetRolesAsync_MapsPersonAndEntityRoles()
    {
        const string body = """
            {
              "rollegrupper": [
                {
                  "type": { "kode": "DAGL", "beskrivelse": "Daglig leder" },
                  "roller": [
                    {
                      "type": { "kode": "DAGL", "beskrivelse": "Daglig leder" },
                      "person": {
                        "navn": { "fornavn": "Jens", "mellomnavn": "Arild", "etternavn": "Gunvaldsen" },
                        "fodselsdato": "1959-08-12"
                      },
                      "fratraadt": false,
                      "avregistrert": false
                    }
                  ]
                },
                {
                  "type": { "kode": "REGN", "beskrivelse": "Regnskapsfører" },
                  "roller": [
                    {
                      "type": { "kode": "REGN", "beskrivelse": "Regnskapsfører" },
                      "enhet": { "organisasjonsnummer": "986252932", "navn": ["DIREKTORATET FOR FORVALTNING"] },
                      "fratraadt": false,
                      "avregistrert": false
                    }
                  ]
                }
              ]
            }
            """;
        _wireMock.Given(Request.Create().WithPath("/enhetsregisteret/api/enheter/974760843/roller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(2);
        result.Roles[0].PersonName.Should().Be("Jens Arild Gunvaldsen");
        result.Roles[0].DateOfBirth.Should().Be(new DateOnly(1959, 8, 12));
        result.Roles[0].RoleType.Should().Be("Daglig leder");
        result.Roles[1].PersonName.Should().Be("DIREKTORATET FOR FORVALTNING (986252932)");
        result.Roles[1].DateOfBirth.Should().BeNull();
    }

    [Fact]
    public async Task GetRolesAsync_FiltersOutRetiredAndDeregistered()
    {
        const string body = """
            {
              "rollegrupper": [
                {
                  "type": { "kode": "STYR" },
                  "roller": [
                    { "type": { "kode": "LEDE", "beskrivelse": "Styrets leder" },
                      "person": { "navn": { "fornavn": "Active", "etternavn": "One" } },
                      "fratraadt": false, "avregistrert": false },
                    { "type": { "kode": "MEDL", "beskrivelse": "Styremedlem" },
                      "person": { "navn": { "fornavn": "Retired", "etternavn": "Person" } },
                      "fratraadt": true, "avregistrert": false },
                    { "type": { "kode": "MEDL", "beskrivelse": "Styremedlem" },
                      "person": { "navn": { "fornavn": "Deregistered", "etternavn": "Person" } },
                      "fratraadt": false, "avregistrert": true }
                  ]
                }
              ]
            }
            """;
        _wireMock.Given(Request.Create().WithPath("/enhetsregisteret/api/enheter/974760843/roller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetRolesAsync(Vegvesenet, CancellationToken.None);

        result!.Roles.Should().HaveCount(1);
        result.Roles[0].PersonName.Should().Be("Active One");
    }

    [Fact]
    public async Task GetRolesAsync_Returns404_ResolvedAsNull()
    {
        _wireMock.Given(Request.Create().WithPath("/enhetsregisteret/api/enheter/974760843/roller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.GetRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().BeNull();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
