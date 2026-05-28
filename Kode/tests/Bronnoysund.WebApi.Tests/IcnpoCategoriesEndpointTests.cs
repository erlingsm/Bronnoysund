// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

public class IcnpoCategoriesEndpointTests
{
    [Fact]
    public async Task GetIcnpoCategories_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetIcnpoCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("01100", "Kultur og kunst"),
                new KodeverkEntry("01200", "Idrett"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/icnpo-kategorier", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
        body[0].Code.Should().Be("01100");
        body[0].Description.Should().Be("Kultur og kunst");
    }

    [Fact]
    public async Task GetIcnpoCategories_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetIcnpoCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Unavailable("Frivillighetsregisteret circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/icnpo-kategorier", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }
}
