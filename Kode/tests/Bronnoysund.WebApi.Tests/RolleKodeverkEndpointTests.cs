// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// H1: End-to-end coverage for the three flat role-kodeverk listings:
/// <c>/kodeverk/rolletyper</c>, <c>/kodeverk/rollegruppetyper</c>, and
/// <c>/kodeverk/representanter</c>. Each happy-path verifies the list shape; one of the
/// three also asserts the 503 ProblemDetails mapping since all three share the same
/// upstream mapping logic.
/// </summary>
public class RolleKodeverkEndpointTests
{
    [Fact]
    public async Task GetRolletyper_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetRolletyperAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("DAGL", "Daglig leder"),
                new KodeverkEntry("STYR", "Styrets leder"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/rolletyper", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body!.Should().HaveCount(2);
        body[0].Code.Should().Be("DAGL");
    }

    [Fact]
    public async Task GetRollegruppetyper_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetRollegruppetyperAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("STYR", "Styret"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/rollegruppetyper", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body!.Should().HaveCount(1);
        body[0].Code.Should().Be("STYR");
    }

    [Fact]
    public async Task GetRepresentanter_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetRepresentanterAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("KONTAKT", "Kontaktperson"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/representanter", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body!.Should().HaveCount(1);
        body[0].Code.Should().Be("KONTAKT");
    }

    [Fact]
    public async Task GetRolletyper_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetRolletyperAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/rolletyper", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }
}
