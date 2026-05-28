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

public class BrregMatrikkelenhetProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregMatrikkelenhetProvider _sut;

    public BrregMatrikkelenhetProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregMatrikkelenhetProvider(brreg, NullLogger<BrregMatrikkelenhetProvider>.Instance);
    }

    [Fact]
    public async Task LookupAsync_MapsListByMatrikkelenhetid()
    {
        const string body = """
            [
              {
                "matrikkelenhetid": "abc-123",
                "orgnr": "974760843",
                "kommnr": "0301",
                "gaardsnr": "1",
                "bruksnr": "1",
                "festenr": "0",
                "rekkefolge": "1"
              },
              {
                "matrikkelenhetid": "abc-124",
                "orgnr": "974760843",
                "kommnr": "0301",
                "gaardsnr": "1",
                "bruksnr": "2",
                "festenr": "0",
                "rekkefolge": "2"
              }
            ]
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "abc-123"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.Found>();
        var items = ((MatrikkelenhetLookupResult.Found)result).Matrikkelenheter;
        items.Should().HaveCount(2);
        items[0].MatrikkelenhetId.Should().Be("abc-123");
        items[0].OrganizationNumber.Should().Be("974760843");
        items[0].KommuneNumber.Should().Be("0301");
        items[0].GardsNumber.Should().Be("1");
        items[0].BruksNumber.Should().Be("1");
        items[0].FesteNumber.Should().Be("0");
        items[0].Order.Should().Be(1);
        items[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task LookupAsync_ReturnsNotFound_OnEmptyArray()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody("[]"));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "xyz"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.NotFound>();
    }

    [Fact]
    public async Task LookupAsync_RejectsBothFiltersAtOnce()
    {
        var result = await _sut.LookupAsync(
            new MatrikkelenhetQuery(MatrikkelenhetId: "abc", Matrikkelnummer: "0301-1/1"),
            CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.InvalidInput>();
    }

    [Fact]
    public async Task LookupAsync_RejectsEmptyQuery()
    {
        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(), CancellationToken.None);
        result.Should().BeOfType<MatrikkelenhetLookupResult.InvalidInput>();
    }

    [Fact]
    public async Task LookupAsync_UsesMatrikkelnummerFilter()
    {
        const string body = """
            [
              { "matrikkelenhetid": "x1", "orgnr": "974760843", "kommnr": "0301", "gaardsnr": "1", "bruksnr": "1" }
            ]
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(Matrikkelnummer: "0301-1/1"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.Found>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "abc"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.Unavailable>();
    }

    [Fact]
    public async Task LookupAsync_TreatsWhitespaceAsUnset()
    {
        // I5: MatrikkelenhetQuery normalises whitespace-only values to null so a query like
        // (MatrikkelenhetId: "   ") is treated identically to no input at all — the adapter's
        // begge-eller-ingen rule then returns InvalidInput with the canonical message.
        var result = await _sut.LookupAsync(
            new MatrikkelenhetQuery(MatrikkelenhetId: "   "),
            CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.InvalidInput>();
    }

    [Fact]
    public async Task LookupAsync_ReturnsNullOrder_OnNonNumericRekkefolge()
    {
        // I9: rekkefolge is parsed with int.TryParse(InvariantCulture); non-numeric values
        // surface as null Order rather than throwing or defaulting to 0.
        const string body = """
            [{ "matrikkelenhetid": "x1", "orgnr": "974760843", "rekkefolge": "ikke-tall" }]
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "x1"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.Found>();
        var items = ((MatrikkelenhetLookupResult.Found)result).Matrikkelenheter;
        items.Should().HaveCount(1);
        items[0].Order.Should().BeNull();
    }

    [Fact]
    public async Task LookupAsync_SkipsEntriesWithMissingKeys()
    {
        // I10: Map drops rows missing matrikkelenhetid or orgnr — verify only the well-formed
        // row survives. The skipped count is also logged (I7) but we do not assert on that here.
        const string body = """
            [
              { "matrikkelenhetid": "x1", "orgnr": "974760843" },
              { "matrikkelenhetid": "", "orgnr": "974760843" },
              { "matrikkelenhetid": "x2", "orgnr": "" }
            ]
            """;
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        var result = await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "x1"), CancellationToken.None);

        result.Should().BeOfType<MatrikkelenhetLookupResult.Found>();
        var items = ((MatrikkelenhetLookupResult.Found)result).Matrikkelenheter;
        items.Should().HaveCount(1);
        items[0].MatrikkelenhetId.Should().Be("x1");
    }

    [Fact]
    public async Task LookupAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/matrikkelenhet").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody("[]"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.LookupAsync(new MatrikkelenhetQuery(MatrikkelenhetId: "abc"), cts.Token);

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
