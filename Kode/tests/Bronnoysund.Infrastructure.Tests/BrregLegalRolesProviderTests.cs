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

public class BrregLegalRolesProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregLegalRolesProvider _sut;
    private static readonly OrganizationNumber Vegvesenet = OrganizationNumber.Create("974760843");

    public BrregLegalRolesProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregLegalRolesProvider(brreg, NullLogger<BrregLegalRolesProvider>.Instance);
    }

    [Fact]
    public async Task GetLegalRolesAsync_MapsHoldingsAndRoles()
    {
        const string body = """
            {
              "organisasjonsnummer": "974760843",
              "erSlettet": false,
              "enheter": [
                {
                  "organisasjonsnummer": "999111222",
                  "navn": "DATTERSELSKAP AS",
                  "roller": [
                    { "type": { "kode": "DTPR", "beskrivelse": "Deltaker" }, "fratraadt": false, "avregistrert": false, "rekkefolge": 1 },
                    { "type": { "kode": "REGN", "beskrivelse": "Regnskapsfører" }, "fratraadt": true, "avregistrert": false, "rekkefolge": 2 }
                  ]
                },
                {
                  "organisasjonsnummer": "555666777",
                  "navn": "REVISJONSKUNDE AS",
                  "roller": [
                    { "type": { "kode": "REVI", "beskrivelse": "Revisor" }, "fratraadt": false, "avregistrert": false }
                  ]
                }
              ]
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/enheter/974760843/juridiskeroller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetLegalRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().BeOfType<LegalRolesLookupResult.Found>();
        var roles = ((LegalRolesLookupResult.Found)result).Roles;
        roles.OrganizationNumber.Should().Be("974760843");
        roles.IsDeleted.Should().BeFalse();
        roles.Holdings.Should().HaveCount(2);

        var first = roles.Holdings[0];
        first.OrganizationNumber.Should().Be("999111222");
        first.Name.Should().Be("DATTERSELSKAP AS");
        first.Roles.Should().HaveCount(2);
        first.Roles[0].TypeCode.Should().Be("DTPR");
        first.Roles[0].TypeDescription.Should().Be("Deltaker");
        first.Roles[0].IsResigned.Should().BeFalse();
        first.Roles[0].Order.Should().Be(1);
        first.Roles[1].TypeCode.Should().Be("REGN");
        first.Roles[1].IsResigned.Should().BeTrue();
    }

    [Fact]
    public async Task GetLegalRolesAsync_ReturnsFoundWithEmptyHoldings_WhenNoRolesElsewhere()
    {
        const string body = """
            {
              "organisasjonsnummer": "974760843",
              "erSlettet": false,
              "enheter": []
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/enheter/974760843/juridiskeroller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetLegalRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().BeOfType<LegalRolesLookupResult.Found>();
        ((LegalRolesLookupResult.Found)result).Roles.Holdings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLegalRolesAsync_ReturnsNotFound_On404()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/enheter/974760843/juridiskeroller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.GetLegalRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().BeOfType<LegalRolesLookupResult.NotFound>();
    }

    [Fact]
    public async Task GetLegalRolesAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/enheter/974760843/juridiskeroller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetLegalRolesAsync(Vegvesenet, CancellationToken.None);

        result.Should().BeOfType<LegalRolesLookupResult.Unavailable>();
    }

    [Fact]
    public async Task GetLegalRolesAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/enheter/974760843/juridiskeroller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{ \"enheter\": [] }"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetLegalRolesAsync(Vegvesenet, cts.Token);

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
