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

public class CompaniesEndpointTests
{
    private static readonly CompanyResponse SampleCompany = new(
        OrganizationNumber: "974760843",
        OrganizationName: "RIKSREVISJONEN",
        CompanyType: "ORGL",
        LanguageForm: "Bokmål");

    [Fact]
    public async Task GetCompany_Found_ReturnsOkWithCompany()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Found(SampleCompany));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        body.Should().NotBeNull();
        body!.OrganizationNumber.Should().Be("974760843");
        body.OrganizationName.Should().Be("RIKSREVISJONEN");
    }

    [Fact]
    public async Task GetCompany_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.NotFound("974760843"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(404);
        problem.title.Should().Be("Company not found");
        problem.type.Should().NotBeNullOrEmpty();
        problem.detail.Should().Contain("974760843");
    }

    [Fact]
    public async Task GetCompany_InvalidOrgnr_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.InvalidInput("Organization number must be 9 digits."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/abc", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(400);
        problem.title.Should().Be("Invalid organization number");
    }

    [Fact]
    public async Task GetCompany_BrregUnavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Company.LookupAsync(Arg.Any<OrganizationNumber>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyLookupResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies/974760843", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(503);
        problem.detail.Should().Contain("circuit open");
    }

    [Fact]
    public async Task SearchByName_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        var result = new CompanySearchResult(
            Hits: [new CompanySearchHit("974760843", "RIKSREVISJONEN", "ORGL", "OSLO")],
            TotalElements: 1);
        factory.Search.SearchByNameAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(result);
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies?name=riksrev", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompanySearchResult>();
        body.Should().NotBeNull();
        body!.Hits.Should().HaveCount(1);
        body.TotalElements.Should().Be(1);
    }

    [Fact]
    public async Task SearchByName_MissingName_Returns400ProblemDetails()
    {
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem.Should().NotBeNull();
        problem!.title.Should().Be("Missing name");
    }

    [Fact]
    public async Task SearchByName_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Search.SearchByNameAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<CompanySearchResult>>(_ => throw new InvalidOperationException("Brreg down"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/companies?name=riksrev", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(503);
    }
}
