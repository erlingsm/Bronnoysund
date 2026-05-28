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

/// <summary>
/// WireMock tests for the iteration-C kodeverk methods on <see cref="BrregKodeverkProvider"/>:
/// Kommuner (paged + single), Rolletyper, Rollegruppetyper, Representanter, single
/// Organisasjonsform, and the two Organisasjonsformer-with-(under)enheter listings.
/// </summary>
public class BrregKodeverkCExtensionsTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregKodeverkProvider _sut;

    public BrregKodeverkCExtensionsTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregKodeverkProvider(brreg, NullLogger<BrregKodeverkProvider>.Instance);
    }

    [Fact]
    public async Task GetKommunerAsync_MapsEntriesAndPagingMetadata()
    {
        const string body = """
            {
              "_embedded": {
                "kommuner": [
                  { "nummer": "0301", "navn": "OSLO" },
                  { "nummer": "1101", "navn": "EIGERSUND" },
                  { "nummer": "1103", "navn": "STAVANGER" }
                ]
              },
              "page": { "size": 3, "totalElements": 5200, "totalPages": 1734, "number": 0 }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/kommuner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetKommunerAsync(page: 0, size: 3, CancellationToken.None);

        result.Should().BeOfType<KodeverkPagedResult.Found>();
        var page = (KodeverkPagedResult.Found)result;
        page.Entries.Should().HaveCount(3);
        page.Entries[0].Code.Should().Be("0301");
        page.Entries[0].Description.Should().Be("OSLO");
        page.TotalElements.Should().Be(5200);
        page.TotalPages.Should().Be(1734);
        page.Page.Should().Be(0);
        page.Size.Should().Be(3);
    }

    [Fact]
    public async Task GetKommunerAsync_ReturnsUnavailable_On503()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/kommuner").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetKommunerAsync(page: 0, size: 100, CancellationToken.None);

        result.Should().BeOfType<KodeverkPagedResult.Unavailable>();
    }

    [Fact]
    public async Task GetKommuneAsync_MapsSingleEntry()
    {
        const string body = """{ "nummer": "0301", "navn": "OSLO" }""";
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/kommuner/0301").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetKommuneAsync("0301", CancellationToken.None);

        result.Should().BeOfType<KodeverkSingleResult.Found>();
        var found = (KodeverkSingleResult.Found)result;
        found.Entry.Code.Should().Be("0301");
        found.Entry.Description.Should().Be("OSLO");
    }

    [Fact]
    public async Task GetKommuneAsync_ReturnsNotFound_On404()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/kommuner/9999").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.GetKommuneAsync("9999", CancellationToken.None);

        result.Should().BeOfType<KodeverkSingleResult.NotFound>();
    }

    [Fact]
    public async Task GetRolletyperAsync_MapsEntries()
    {
        const string body = """
            {
              "_embedded": {
                "rolletyper": [
                  { "kode": "DAGL", "beskrivelse": "Daglig leder" },
                  { "kode": "STYR", "beskrivelse": "Styrets leder" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/rolletyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetRolletyperAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(2);
        entries[0].Code.Should().Be("DAGL");
        entries[0].Description.Should().Be("Daglig leder");
        entries[1].Code.Should().Be("STYR");
    }

    [Fact]
    public async Task GetRollegruppetyperAsync_MapsEntries()
    {
        const string body = """
            {
              "_embedded": {
                "rollegruppetyper": [
                  { "kode": "STYR", "beskrivelse": "Styret" },
                  { "kode": "DAGL", "beskrivelse": "Daglig leder" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/rollegruppetyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetRollegruppetyperAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        ((KodeverkLookupResult.Found)result).Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRepresentanterAsync_MapsEntries()
    {
        const string body = """
            {
              "_embedded": {
                "representanter": [
                  { "kode": "KONTAKT", "beskrivelse": "Kontaktperson" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/representanter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetRepresentanterAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        var entries = ((KodeverkLookupResult.Found)result).Entries;
        entries.Should().HaveCount(1);
        entries[0].Code.Should().Be("KONTAKT");
    }

    [Fact]
    public async Task GetOrganisasjonsformAsync_MapsSingleEntryAndRetiredFlag()
    {
        const string body = """
            { "kode": "AS", "beskrivelse": "Aksjeselskap" }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/AS").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetOrganisasjonsformAsync("AS", CancellationToken.None);

        result.Should().BeOfType<KodeverkSingleResult.Found>();
        var found = (KodeverkSingleResult.Found)result;
        found.Entry.Code.Should().Be("AS");
        found.Entry.Description.Should().Be("Aksjeselskap");
        found.Entry.IsRetired.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrganisasjonsformAsync_MarksRetired_WhenUtgaattSet()
    {
        const string body = """
            { "kode": "OBS", "beskrivelse": "Old form", "utgaatt": "1995-01-01" }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/OBS").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetOrganisasjonsformAsync("OBS", CancellationToken.None);

        result.Should().BeOfType<KodeverkSingleResult.Found>();
        ((KodeverkSingleResult.Found)result).Entry.IsRetired.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganisasjonsformAsync_ReturnsNotFound_On404()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/XYZ").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var result = await _sut.GetOrganisasjonsformAsync("XYZ", CancellationToken.None);

        result.Should().BeOfType<KodeverkSingleResult.NotFound>();
    }

    [Fact]
    public async Task GetOrganisasjonsformerWithEnheterAsync_MapsEntries()
    {
        const string body = """
            {
              "_embedded": {
                "organisasjonsformer": [
                  { "kode": "AS", "beskrivelse": "Aksjeselskap" },
                  { "kode": "ENK", "beskrivelse": "Enkeltpersonforetak" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/enheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetOrganisasjonsformerWithEnheterAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        ((KodeverkLookupResult.Found)result).Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrganisasjonsformerWithUnderenheterAsync_MapsEntries()
    {
        const string body = """
            {
              "_embedded": {
                "organisasjonsformer": [
                  { "kode": "BEDR", "beskrivelse": "Bedrift" }
                ]
              }
            }
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/underenheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.GetOrganisasjonsformerWithUnderenheterAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Found>();
        ((KodeverkLookupResult.Found)result).Entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRolletyperAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/rolletyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"_embedded":{"rolletyper":[]}}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetRolletyperAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetRolletyperAsync_ReturnsUnavailable_On503_ViaHelper()
    {
        // H10: Verify the shared GetSimpleListAsync helper's ApiException arm maps a 5xx
        // upstream into Unavailable. Rolletyper is one of five methods routed through the
        // helper, so a single test covers the path for Rollegruppetyper, Representanter,
        // and the two Organisasjonsformer-med-(under)enheter listings as well.
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/rolletyper").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetRolletyperAsync(CancellationToken.None);

        result.Should().BeOfType<KodeverkLookupResult.Unavailable>();
    }

    [Fact]
    public async Task GetKommuneAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/kommuner/0301").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"nummer":"0301","navn":"OSLO"}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetKommuneAsync("0301", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetOrganisasjonsformAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/organisasjonsformer/AS").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"kode":"AS","beskrivelse":"Aksjeselskap"}"""));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetOrganisasjonsformAsync("AS", cts.Token);

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
