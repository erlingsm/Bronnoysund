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

public class VoluntaryOrganizationSearchEndpointTests
{
    [Fact]
    public async Task GetVoluntarySearch_Found_ReturnsOkWithCursor()
    {
        using var factory = new WebApiFactory();
        var hits = new VoluntaryOrganizationSearchResponse(
            Organizations: [
                new VoluntaryOrganizationResponse(
                    "974760843", "AKTIV", null, null, "01100", "Kultur", true, "12345678901"),
                new VoluntaryOrganizationResponse(
                    "987654321", "AKTIV", null, null, "02000", "Idrett", false, null),
            ],
            NextCursor: "ABC==");
        factory.VoluntarySearch.SearchAsync(Arg.Any<VoluntaryOrganizationSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationSearchResult.Found(hits));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations?size=2", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VoluntaryOrganizationSearchResponse>();
        body.Should().NotBeNull();
        body!.Organizations.Should().HaveCount(2);
        body.NextCursor.Should().Be("ABC==");
    }

    [Fact]
    public async Task GetVoluntarySearch_InvalidSize_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.VoluntarySearch.SearchAsync(Arg.Any<VoluntaryOrganizationSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationSearchResult.InvalidInput("Size must be between 1 and 100 (got 0)."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations?size=0", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
    }

    [Fact]
    public async Task GetVoluntarySearch_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.VoluntarySearch.SearchAsync(Arg.Any<VoluntaryOrganizationSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationSearchResult.Unavailable("Frivillighetsregisteret circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations?size=20", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }

    [Fact]
    public async Task GetVoluntarySearch_RejectsMalformedSearchAfter_Returns400()
    {
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations?searchAfter=" + Uri.EscapeDataString("bad value!"), UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
        // The upstream provider should never have been hit when validation rejected at the edge.
        await factory.VoluntarySearch.DidNotReceive().SearchAsync(
            Arg.Any<VoluntaryOrganizationSearchQuery>(), Arg.Any<CancellationToken>());
    }
}
