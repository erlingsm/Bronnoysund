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

public class BrregEntityChangesProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregEntityChangesProvider _sut;
    private static readonly OrganizationNumber Vegvesenet = OrganizationNumber.Create("974760843");

    public BrregEntityChangesProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregEntityChangesProvider(brreg, NullLogger<BrregEntityChangesProvider>.Instance);
    }

    [Fact]
    public async Task GetChangesAsync_FansOutThreeFeeds_AndMergesResults()
    {
        StubEntityFeed("""
            {
              "_embedded": {
                "oppdaterteEnheter": [
                  { "oppdateringsid": 11111, "dato": "2026-05-01T10:00:00Z", "organisasjonsnummer": "974760843", "endringstype": "Endring" }
                ]
              },
              "page": { "totalElements": 1 }
            }
            """);
        StubSubUnitFeed("""
            {
              "_embedded": {
                "oppdaterteUnderenheter": [
                  { "oppdateringsid": 22222, "dato": "2026-05-02T11:00:00Z", "organisasjonsnummer": "923802029", "endringstype": "Sletting" }
                ]
              },
              "page": { "totalElements": 1 }
            }
            """);
        StubRoleFeed("""
            [
              { "id": "abc-123", "source": "RollerEndret", "specversion": "1.0", "time": "2026-05-03T12:00:00Z", "data": { "organisasjonsnummer": "974760843" } }
            ]
            """);

        var result = await _sut.GetChangesAsync(Vegvesenet, pageSize: 20, CancellationToken.None);

        result.OrganizationNumber.Should().Be("974760843");
        result.EntityFeed.ErrorMessage.Should().BeNull();
        result.EntityFeed.Changes.Should().HaveCount(1);
        result.EntityFeed.Changes[0].UpdateId.Should().Be(11111);
        result.EntityFeed.Changes[0].ChangeType.Should().Be("Endring");

        result.SubUnitFeed.Changes.Should().HaveCount(1);
        result.SubUnitFeed.Changes[0].UpdateId.Should().Be(22222);
        result.SubUnitFeed.Changes[0].ChangeType.Should().Be("Sletting");

        result.RoleFeed.Changes.Should().HaveCount(1);
        result.RoleFeed.Changes[0].ChangeType.Should().Be("RollerEndret");
        result.RoleFeed.Changes[0].Timestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChangesAsync_MapsAllFields_OnEachFeed()
    {
        // Two events per feed to verify both field-mapping and rekkefølge-bevaring.
        StubEntityFeed("""
            {
              "_embedded": {
                "oppdaterteEnheter": [
                  { "oppdateringsid": 1001, "dato": "2026-05-01T10:00:00Z", "organisasjonsnummer": "974760843", "endringstype": "Endring" },
                  { "oppdateringsid": 1002, "dato": "2026-05-04T08:30:00Z", "organisasjonsnummer": "974760843", "endringstype": "Sletting" }
                ]
              },
              "page": { "totalElements": 2 }
            }
            """);
        StubSubUnitFeed("""
            {
              "_embedded": {
                "oppdaterteUnderenheter": [
                  { "oppdateringsid": 2001, "dato": "2026-05-02T11:00:00Z", "organisasjonsnummer": "923802029", "endringstype": "Endring" },
                  { "oppdateringsid": 2002, "dato": "2026-05-05T09:15:00Z", "organisasjonsnummer": "923802029", "endringstype": "Sletting" }
                ]
              },
              "page": { "totalElements": 2 }
            }
            """);
        StubRoleFeed("""
            [
              { "id": "evt-1", "source": "RollerEndret", "specversion": "1.0", "time": "2026-05-03T12:00:00Z", "data": { "organisasjonsnummer": "974760843" } },
              { "id": "evt-2", "source": "RollerSlettet", "specversion": "1.0", "time": "2026-05-06T07:45:00Z", "data": { "organisasjonsnummer": "974760843" } }
            ]
            """);

        var result = await _sut.GetChangesAsync(Vegvesenet, pageSize: 20, CancellationToken.None);

        // Entity feed — Timestamp from "dato" (ISO-8601 Z), ChangeType from "endringstype",
        // UpdateId from "oppdateringsid", order preserved.
        result.EntityFeed.Changes.Should().HaveCount(2);
        result.EntityFeed.Changes[0].UpdateId.Should().Be(1001);
        result.EntityFeed.Changes[0].ChangeType.Should().Be("Endring");
        result.EntityFeed.Changes[0].Timestamp.Should().Be(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
        result.EntityFeed.Changes[1].UpdateId.Should().Be(1002);
        result.EntityFeed.Changes[1].ChangeType.Should().Be("Sletting");
        result.EntityFeed.Changes[1].Timestamp.Should().Be(new DateTimeOffset(2026, 5, 4, 8, 30, 0, TimeSpan.Zero));

        // Sub-unit feed — same mapping.
        result.SubUnitFeed.Changes.Should().HaveCount(2);
        result.SubUnitFeed.Changes[0].UpdateId.Should().Be(2001);
        result.SubUnitFeed.Changes[0].ChangeType.Should().Be("Endring");
        result.SubUnitFeed.Changes[0].Timestamp.Should().Be(new DateTimeOffset(2026, 5, 2, 11, 0, 0, TimeSpan.Zero));
        result.SubUnitFeed.Changes[1].UpdateId.Should().Be(2002);
        result.SubUnitFeed.Changes[1].ChangeType.Should().Be("Sletting");

        // Role feed — Timestamp from "time", ChangeType from "source", UpdateId always null,
        // order preserved.
        result.RoleFeed.Changes.Should().HaveCount(2);
        result.RoleFeed.Changes[0].ChangeType.Should().Be("RollerEndret");
        result.RoleFeed.Changes[0].UpdateId.Should().BeNull();
        result.RoleFeed.Changes[0].Timestamp.Should().Be(new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));
        result.RoleFeed.Changes[1].ChangeType.Should().Be("RollerSlettet");
        result.RoleFeed.Changes[1].UpdateId.Should().BeNull();
    }

    [Fact]
    public async Task GetChangesAsync_PropagatesCancellation_WhenTokenAlreadyCancelled()
    {
        StubEntityFeed("""{ "_embedded": { "oppdaterteEnheter": [] }, "page": {} }""");
        StubSubUnitFeed("""{ "_embedded": { "oppdaterteUnderenheter": [] }, "page": {} }""");
        StubRoleFeed("[]");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.GetChangesAsync(Vegvesenet, pageSize: 20, cts.Token);

        // After F6: ShouldIsolate returns false when ct.IsCancellationRequested, so the
        // TaskCanceledException must propagate as OperationCanceledException rather than
        // being silently demoted to a per-feed error message.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetChangesAsync_IsolatesErrors_OnFailingFeed()
    {
        StubEntityFeed("""{ "_embedded": { "oppdaterteEnheter": [] }, "page": {} }""");
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/oppdateringer/underenheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));
        StubRoleFeed("[]");

        var result = await _sut.GetChangesAsync(Vegvesenet, pageSize: 20, CancellationToken.None);

        result.EntityFeed.ErrorMessage.Should().BeNull();
        result.SubUnitFeed.ErrorMessage.Should().NotBeNull();
        result.SubUnitFeed.ErrorMessage.Should().Contain("/oppdateringer/underenheter");
        result.SubUnitFeed.Changes.Should().BeEmpty();
        result.RoleFeed.ErrorMessage.Should().BeNull();
    }

    private void StubEntityFeed(string body) =>
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/oppdateringer/enheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

    private void StubSubUnitFeed(string body) =>
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/oppdateringer/underenheter").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

    private void StubRoleFeed(string body) =>
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/oppdateringer/roller").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
