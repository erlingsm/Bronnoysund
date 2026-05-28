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

public class BrregVoluntaryOrganizationProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregVoluntaryOrganizationProvider _sut;
    private static readonly OrganizationNumber Org = OrganizationNumber.Create("974760843");

    public BrregVoluntaryOrganizationProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var factory = new SingleClientHttpClientFactory(_httpClient);
        _sut = new BrregVoluntaryOrganizationProvider(factory, NullLogger<BrregVoluntaryOrganizationProvider>.Instance);
    }

    private sealed class SingleClientHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            // Adapter creates its HttpClient via factory.CreateClient(nameof(BrregClient)) — we
            // hard-code the string here to avoid leaking the Generated typed-client into the
            // test project. If the adapter's client name changes, this test will fail fast.
            if (!string.Equals(name, "BrregClient", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected client name: {name}", nameof(name));
            }
            return client;
        }
    }

    [Fact]
    public async Task LookupAsync_MapsInnfoertOrganization()
    {
        const string body = """
            {
              "respons_klasse": "FrivilligOrganisasjonInnfoert",
              "organisasjonsnummer": "974760843",
              "frivilligOrganisasjonsstatus": "AKTIV",
              "foersteGangInnfoert": "2010-04-15",
              "innfoertDato": "2024-03-01",
              "kontonummer": "12345678901",
              "icnpoKategorier": [
                { "icnpoNummer": "01100", "navn": "Kultur og kunst", "rekkefoelge": 1 },
                { "icnpoNummer": "12200", "navn": "Annet", "rekkefoelge": 2 }
              ],
              "grasrotandel": { "deltarI": true }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner/974760843").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.LookupAsync(Org, CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationLookupResult.Found>();
        var org = ((VoluntaryOrganizationLookupResult.Found)result).Organization;
        org.OrganizationNumber.Should().Be("974760843");
        org.Status.Should().Be("AKTIV");
        org.FirstRegisteredDate.Should().Be(new DateOnly(2010, 4, 15));
        org.RegisteredDate.Should().Be(new DateOnly(2024, 3, 1));
        org.AccountNumber.Should().Be("12345678901");
        org.PrimaryIcnpoCategoryNumber.Should().Be("01100");
        org.PrimaryIcnpoCategoryName.Should().Be("Kultur og kunst");
        org.ParticipatesInGrasrotandel.Should().BeTrue();
    }

    [Fact]
    public async Task LookupAsync_ReturnsNotRegistered_ForSlettetOrganisasjon()
    {
        const string body = """
            {
              "respons_klasse": "FrivilligOrganisasjonSlettet",
              "organisasjonsnummer": "974760843"
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner/974760843").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/hal+json").WithBody(body));

        var result = await _sut.LookupAsync(Org, CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationLookupResult.NotRegistered>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsNotRegistered_On404()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner/974760843").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.LookupAsync(Org, CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationLookupResult.NotRegistered>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner/974760843").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.LookupAsync(Org, CancellationToken.None);

        result.Should().BeOfType<VoluntaryOrganizationLookupResult.Unavailable>();
    }

    [Fact]
    public async Task LookupAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/frivillighetsregisteret/api/frivillige-organisasjoner/974760843").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.LookupAsync(Org, cts.Token);

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
