// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

public class AggregatedEndpointTests
{
    private static readonly CompanyResponse SampleCompany = new(
        "974760843", "RIKSREVISJONEN", "ORGL", "Bokmål");

    [Fact]
    public async Task GetAggregated_Found_NoEnrichmentByDefault()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(SampleCompany));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/aggregated", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AggregatedCompanyResponse>();
        body.Should().NotBeNull();
        body!.Core.OrganizationNumber.Should().Be("974760843");
        body.Voluntary.Should().BeNull();
        body.LegalRoles.Should().BeNull();
        body.Changes.Should().BeNull();
        body.SubUnitDetails.Should().BeNull();

        // Default scope must NOT fan out to the enrichment ports.
        await factory.Voluntary.DidNotReceive().LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
        await factory.LegalRoles.DidNotReceive().GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
        await factory.EntityChanges.DidNotReceive().GetChangesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await factory.SubUnitDetails.DidNotReceive().LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAggregated_WithEnrichment_PopulatesAllFourEnrichmentFields()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(SampleCompany));

        var voluntary = new VoluntaryOrganizationResponse(
            "974760843", "REGISTRERT", null, null, null, null, false, null);
        var legalRoles = new LegalRolesResponse("974760843", IsDeleted: false, Holdings: []);
        var changes = new EntityChangesResponse(
            "974760843",
            new EntityChangesFeed([]),
            new EntityChangesFeed([]),
            new EntityChangesFeed([]));
        var subUnit = new SubUnitDetailsResponse("974760843", "RIKSREVISJONEN", "974760843");

        factory.Voluntary.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new VoluntaryOrganizationLookupResult.Found(voluntary));
        factory.LegalRoles.GetLegalRolesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new LegalRolesLookupResult.Found(legalRoles));
        factory.EntityChanges.GetChangesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(changes);
        factory.SubUnitDetails.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new SubUnitLookupResult.Found(subUnit));

        var client = factory.CreateClient();
        var response = await client.GetAsync(
            new Uri("/companies/974760843/aggregated?include=enrichment", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AggregatedCompanyResponse>();
        body.Should().NotBeNull();
        body!.Voluntary.Should().NotBeNull();
        body.LegalRoles.Should().NotBeNull();
        body.Changes.Should().NotBeNull();
        body.SubUnitDetails.Should().NotBeNull();
        await factory.Voluntary.Received(1).LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAggregated_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("974760843"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/aggregated", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Company not found");
    }

    [Fact]
    public async Task GetAggregated_InvalidOrgnr_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/abc/aggregated", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid organization number");
    }
}
