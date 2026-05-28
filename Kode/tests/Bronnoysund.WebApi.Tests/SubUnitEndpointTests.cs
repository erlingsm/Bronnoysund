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

public class SubUnitEndpointTests
{
    [Fact]
    public async Task GetSubUnit_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        var subUnit = new SubUnitDetailsResponse(
            "974707314",
            "RIKSREVISJONEN - AVD A",
            "974760843");
        factory.SubUnitDetails.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.Found(subUnit));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/sub-units/974707314", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubUnitDetailsResponse>();
        body.Should().NotBeNull();
        body!.OrganizationNumber.Should().Be("974707314");
        body.ParentOrganizationNumber.Should().Be("974760843");
    }

    [Fact]
    public async Task GetSubUnit_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.SubUnitDetails.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.NotFound("974707314"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/sub-units/974707314", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Sub-unit not found");
    }

    [Fact]
    public async Task GetSubUnit_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.SubUnitDetails.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.Unavailable("Brreg down"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/sub-units/974707314", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
