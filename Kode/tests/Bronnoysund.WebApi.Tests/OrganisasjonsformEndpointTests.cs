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
/// H1: End-to-end coverage for the three organisasjonsform-endepunkts:
/// single-by-code, with-enheter and with-underenheter listings.
/// </summary>
public class OrganisasjonsformEndpointTests
{
    [Fact]
    public async Task GetOrganisasjonsformSingle_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetOrganisasjonsformAsync("AS", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.Found(new KodeverkEntry("AS", "Aksjeselskap")));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/organisasjonsformer/AS", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<KodeverkEntry>();
        body!.Code.Should().Be("AS");
        body.Description.Should().Be("Aksjeselskap");
    }

    [Fact]
    public async Task GetOrganisasjonsformSingle_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetOrganisasjonsformAsync("XYZ", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.NotFound("XYZ"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/organisasjonsformer/XYZ", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Organisasjonsform not found");
        problem.detail.Should().Contain("XYZ");
    }

    [Fact]
    public async Task GetOrganisasjonsformSingle_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetOrganisasjonsformAsync("AS", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/organisasjonsformer/AS", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }

    [Fact]
    public async Task GetOrganisasjonsformSingle_LowerCase_Returns400ProblemDetails()
    {
        // H11: Codes must be upper-case alphanumeric; lower-case is rejected at the edge.
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/organisasjonsformer/as", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await factory.Kodeverk.DidNotReceive().GetOrganisasjonsformAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrganisasjonsformerWithEnheter_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetOrganisasjonsformerWithEnheterAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("AS", "Aksjeselskap"),
                new KodeverkEntry("ENK", "Enkeltpersonforetak"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/kodeverk/organisasjonsformer-med-enheter", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body!.Should().HaveCount(2);
        body[0].Code.Should().Be("AS");
    }

    [Fact]
    public async Task GetOrganisasjonsformerWithUnderenheter_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetOrganisasjonsformerWithUnderenheterAsync(Arg.Any<CancellationToken>())
            .Returns(new KodeverkLookupResult.Found([
                new KodeverkEntry("BEDR", "Bedrift"),
            ]));
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/kodeverk/organisasjonsformer-med-underenheter", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<KodeverkEntry>>();
        body!.Should().HaveCount(1);
        body[0].Code.Should().Be("BEDR");
    }
}
