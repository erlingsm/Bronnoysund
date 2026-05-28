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

public class EntityChangesEndpointTests
{
    private static readonly CompanyResponse SampleCompany = new(
        "974760843", "RIKSREVISJONEN", "ORGL", "Bokmål");

    [Fact]
    public async Task GetChanges_CompanyExists_ReturnsThreeFeeds()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(SampleCompany));
        var changes = new EntityChangesResponse(
            "974760843",
            new EntityChangesFeed([new EntityChange(DateTimeOffset.UtcNow, "Endring", 1)]),
            new EntityChangesFeed([]),
            new EntityChangesFeed([]));
        factory.EntityChanges.GetChangesAsync(Arg.Any<OrganizationNumber>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(changes);
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/changes", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EntityChangesResponse>();
        body.Should().NotBeNull();
        body!.EntityFeed.Changes.Should().HaveCount(1);
        body.SubUnitFeed.Changes.Should().BeEmpty();
        body.RoleFeed.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChanges_CompanyNotFound_Returns404ProblemDetails()
    {
        // F1-fix: the changes endpoint must short-circuit on NotFound to avoid the
        // misleading "200 OK with empty feeds" behaviour from the raw Brreg endpoint.
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("974760843"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/changes", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Company not found");
    }

    [Fact]
    public async Task GetChanges_CompanyProviderUnavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Unavailable("Brreg down"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843/changes", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
