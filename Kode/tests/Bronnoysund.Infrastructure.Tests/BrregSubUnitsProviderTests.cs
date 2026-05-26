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

public class BrregSubUnitsProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregSubUnitsProvider _sut;
    private static readonly OrganizationNumber Vegvesenet = OrganizationNumber.Create("974760843");

    public BrregSubUnitsProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = new BrregHttpClient(_httpClient, NullLogger<BrregHttpClient>.Instance);
        _sut = new BrregSubUnitsProvider(brreg, NullLogger<BrregSubUnitsProvider>.Instance);
    }

    [Fact]
    public async Task GetSubUnitsAsync_MapsEmbeddedList()
    {
        const string body = """
            {
              "_embedded": {
                "underenheter": [
                  { "organisasjonsnummer": "974707314", "navn": "RIKSREVISJONEN" },
                  { "organisasjonsnummer": "923802029", "navn": "RIKSREVISJONEN AVD BERGEN" }
                ]
              },
              "page": { "totalElements": 2, "totalPages": 1, "number": 0, "size": 100 }
            }
            """;
        _wireMock.Given(Request.Create().WithPath("/underenheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetSubUnitsAsync(Vegvesenet, CancellationToken.None);

        result.Should().NotBeNull();
        result!.SubUnits.Should().HaveCount(2);
        result.SubUnits[0].OrganizationNumber.Should().Be("974707314");
        result.SubUnits[0].Name.Should().Be("RIKSREVISJONEN");
    }

    [Fact]
    public async Task GetSubUnitsAsync_EmptyEmbedded_ReturnsEmptyList()
    {
        const string body = """{"_embedded": {"underenheter": []}, "page": {"totalElements": 0}}""";
        _wireMock.Given(Request.Create().WithPath("/underenheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetSubUnitsAsync(Vegvesenet, CancellationToken.None);

        result.Should().NotBeNull();
        result!.SubUnits.Should().BeEmpty();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
