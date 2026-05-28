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

public class BrregStatisticsProviderTests : IDisposable
{
    private readonly WireMockServer _wireMock;
    private readonly HttpClient _httpClient;
    private readonly BrregStatisticsProvider _sut;

    public BrregStatisticsProviderTests()
    {
        _wireMock = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMock.Url!) };
        var brreg = TestKiotaClientFactory.ForWireMock(_wireMock);
        _sut = new BrregStatisticsProvider(brreg, NullLogger<BrregStatisticsProvider>.Instance);
    }

    [Fact]
    public async Task GetRolesTotalCountAsync_ParsesPlainIntegerBody()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/totalbestand").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain").WithBody("4711000"));

        var result = await _sut.GetRolesTotalCountAsync(CancellationToken.None);

        result.Should().BeOfType<RolesTotalCountResult.Found>();
        ((RolesTotalCountResult.Found)result).TotalCount.Should().Be(4711000);
    }

    [Fact]
    public async Task GetRolesTotalCountAsync_TrimsWhitespace()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/totalbestand").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain").WithBody("  12345\n"));

        var result = await _sut.GetRolesTotalCountAsync(CancellationToken.None);

        result.Should().BeOfType<RolesTotalCountResult.Found>();
        ((RolesTotalCountResult.Found)result).TotalCount.Should().Be(12345);
    }

    [Fact]
    public async Task GetRolesTotalCountAsync_ReturnsUnavailable_OnNonIntegerBody()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/totalbestand").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain").WithBody("not a number"));

        var result = await _sut.GetRolesTotalCountAsync(CancellationToken.None);

        result.Should().BeOfType<RolesTotalCountResult.Unavailable>();
    }

    [Fact]
    public async Task GetRolesTotalCountAsync_ReturnsUnavailable_On5xx()
    {
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/totalbestand").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var result = await _sut.GetRolesTotalCountAsync(CancellationToken.None);

        result.Should().BeOfType<RolesTotalCountResult.Unavailable>();
    }

    [Fact]
    public async Task GetRolesTotalCountAsync_ReturnsUnavailable_OnEmptyBody()
    {
        // H3: A 200 OK with an empty body (gateway bug, mis-configured Brreg edge) must not
        // crash long.TryParse and must surface as Unavailable, not as Found(0).
        _wireMock.Given(Request.Create()
                .WithPath("/enhetsregisteret/api/roller/totalbestand").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain").WithBody(""));

        var result = await _sut.GetRolesTotalCountAsync(CancellationToken.None);

        result.Should().BeOfType<RolesTotalCountResult.Unavailable>();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMock.Stop();
        _wireMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
