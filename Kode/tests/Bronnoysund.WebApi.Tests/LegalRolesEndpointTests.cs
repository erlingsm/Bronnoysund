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

public class LegalRolesEndpointTests
{
    [Fact]
    public async Task GetLegalRoles_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        var legalRoles = new LegalRolesResponse(
            "974760843",
            IsDeleted: false,
            Holdings: [new LegalRoleHolding(
                "111222333",
                "DATTERSELSKAP AS",
                [new LegalRoleAssignment("DAGL", "Daglig leder", false, false, 1)])]);
        factory.LegalRoles.GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.Found(legalRoles));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/legal-roles", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LegalRolesResponse>();
        body.Should().NotBeNull();
        body!.Holdings.Should().HaveCount(1);
        body.Holdings[0].OrganizationNumber.Should().Be("111222333");
    }

    [Fact]
    public async Task GetLegalRoles_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.LegalRoles.GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.NotFound("974760843"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/legal-roles", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Legal roles not found");
    }

    [Fact]
    public async Task GetLegalRoles_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.LegalRoles.GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.Unavailable("circuit open"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/legal-roles", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
