// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// D2: End-to-end coverage for <c>GET /matrikkelenhet</c>. Covers the happy paths for both
/// query modes, the begge-eller-ingen edge validation, NotFound → 404, Unavailable → 503,
/// and the I2 edge-level format-validation arms (malformed matrikkelenhetid / matrikkelnummer).
/// </summary>
public class MatrikkelenhetEndpointTests
{
    private static MatrikkelenhetResponse SampleRow(string id = "abc-123") =>
        new(MatrikkelenhetId: id,
            OrganizationNumber: "974760843",
            KommuneNumber: "0301",
            GardsNumber: "1",
            BruksNumber: "1",
            FesteNumber: "0",
            Order: 1);

    [Fact]
    public async Task GetMatrikkelenhet_ByMatrikkelenhetId_ReturnsOkArray()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.Found([SampleRow("abc-123")]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelenhetid=abc-123", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MatrikkelenhetResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].MatrikkelenhetId.Should().Be("abc-123");
    }

    [Fact]
    public async Task GetMatrikkelenhet_ByMatrikkelnummer_ReturnsOkArray()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.Found([SampleRow("abc-124"), SampleRow("abc-125")]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelnummer=0301-1/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MatrikkelenhetResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMatrikkelenhet_BothQueryParamsSet_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.InvalidInput(
                "Provide exactly one of MatrikkelenhetId or Matrikkelnummer — not both."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/matrikkelenhet?matrikkelenhetid=abc-123&matrikkelnummer=0301-1/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
    }

    [Fact]
    public async Task GetMatrikkelenhet_NoQueryParams_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.InvalidInput(
                "Either MatrikkelenhetId or Matrikkelnummer must be provided."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
    }

    [Fact]
    public async Task GetMatrikkelenhet_EmptyList_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.NotFound("abc-999"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelenhetid=abc-999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Matrikkelenhet not found");
        problem.detail.Should().Contain("abc-999");
    }

    [Fact]
    public async Task GetMatrikkelenhet_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Matrikkelenhet.LookupAsync(Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>())
            .Returns(new MatrikkelenhetLookupResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelenhetid=abc-123", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }

    [Fact]
    public async Task GetMatrikkelenhet_MalformedMatrikkelenhetId_Returns400_WithoutCallingProvider()
    {
        // I2: edge-level regex rejects characters outside [A-Za-z0-9_-]. The provider must not
        // be hit when the input is structurally invalid.
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelenhetid=abc%20123", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
        problem.detail.Should().Contain("matrikkelenhetid");
        await factory.Matrikkelenhet.DidNotReceive().LookupAsync(
            Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMatrikkelenhet_MalformedMatrikkelnummer_Returns400_WithoutCallingProvider()
    {
        // I2: matrikkelnummer must match kommune-gnr/bnr[/fnr]. "abc/123" violates the kommune
        // 2-4-digit prefix so the validator rejects.
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/matrikkelenhet?matrikkelnummer=abc/123", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
        problem.detail.Should().Contain("matrikkelnummer");
        await factory.Matrikkelenhet.DidNotReceive().LookupAsync(
            Arg.Any<MatrikkelenhetQuery>(), Arg.Any<CancellationToken>());
    }
}
