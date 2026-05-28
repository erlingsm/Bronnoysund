// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

public class VoluntaryOrganizationEndpointTests
{
    [Fact]
    public async Task GetVoluntary_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        var voluntary = new VoluntaryOrganizationResponse(
            "974760843",
            "REGISTRERT",
            FirstRegisteredDate: new DateOnly(2020, 1, 1),
            RegisteredDate: new DateOnly(2024, 3, 1),
            PrimaryIcnpoCategoryNumber: "1",
            PrimaryIcnpoCategoryName: "Kultur og fritid",
            ParticipatesInGrasrotandel: true,
            AccountNumber: "12345678901");
        factory.Voluntary.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.Found(voluntary));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VoluntaryOrganizationResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("REGISTRERT");
        body.ParticipatesInGrasrotandel.Should().BeTrue();
    }

    [Fact]
    public async Task GetVoluntary_NotRegistered_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Voluntary.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.NotRegistered("974760843"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Not registered in Frivillighetsregisteret");
    }

    [Fact]
    public async Task GetVoluntary_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Voluntary.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.Unavailable("nede"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/voluntary-organizations/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
