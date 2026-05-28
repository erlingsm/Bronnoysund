// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// H1: End-to-end coverage for <c>/kodeverk/kommuner</c> (paged list) and
/// <c>/kodeverk/kommuner/{kommunenummer}</c> (single). Verifies the paged JSON shape,
/// size clamping at 100, NotFound → 404, and upstream Unavailable → 503.
/// </summary>
public class KommunerEndpointTests
{
    [Fact]
    public async Task GetKommunerPaged_Found_ReturnsOkWithPagingShape()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetKommunerAsync(0, 2, Arg.Any<CancellationToken>())
            .Returns(new KodeverkPagedResult.Found(
                Entries: [
                    new KodeverkEntry("0301", "OSLO"),
                    new KodeverkEntry("1101", "EIGERSUND"),
                ],
                Page: 0,
                Size: 2,
                TotalElements: 5200,
                TotalPages: 2600));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner?page=0&size=2", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("entries").GetArrayLength().Should().Be(2);
        root.GetProperty("entries")[0].GetProperty("code").GetString().Should().Be("0301");
        root.GetProperty("entries")[0].GetProperty("description").GetString().Should().Be("OSLO");
        root.GetProperty("page").GetInt32().Should().Be(0);
        root.GetProperty("size").GetInt32().Should().Be(2);
        root.GetProperty("totalElements").GetInt32().Should().Be(5200);
        root.GetProperty("totalPages").GetInt32().Should().Be(2600);
    }

    [Fact]
    public async Task GetKommunerPaged_SizeAbove100_ClampsTo100()
    {
        using var factory = new WebApiFactory();
        var capturedSize = 0;
        factory.Kodeverk.GetKommunerAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedSize = ci.ArgAt<int>(1);
                return new KodeverkPagedResult.Found([], 0, capturedSize, 0, 0);
            });
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner?size=200", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedSize.Should().Be(100, "the endpoint must clamp size to the Brreg server-side cap of 100");
    }

    [Fact]
    public async Task GetKommunerPaged_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetKommunerAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new KodeverkPagedResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }

    [Fact]
    public async Task GetKommuneSingle_Found_ReturnsOk()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetKommuneAsync("0301", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.Found(new KodeverkEntry("0301", "OSLO")));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner/0301", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<KodeverkEntry>();
        body!.Code.Should().Be("0301");
        body.Description.Should().Be("OSLO");
    }

    [Fact]
    public async Task GetKommuneSingle_NotFound_Returns404ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetKommuneAsync("9999", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.NotFound("9999"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner/9999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Kommune not found");
        problem.detail.Should().Contain("9999");
    }

    [Fact]
    public async Task GetKommuneSingle_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Kodeverk.GetKommuneAsync("0301", Arg.Any<CancellationToken>())
            .Returns(new KodeverkSingleResult.Unavailable("Brreg unavailable."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner/0301", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetKommuneSingle_InvalidFormat_Returns400ProblemDetails()
    {
        // H11: 1 digit is below the 2-4 digit minimum so the edge validator rejects.
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.title.Should().Be("Invalid input");
        // Upstream must not have been hit when validation rejected at the edge.
        await factory.Kodeverk.DidNotReceive().GetKommuneAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetKommuneSingle_LettersInPath_Returns400ProblemDetails()
    {
        // H11: Non-digit input is rejected before reaching Brreg.
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/kodeverk/kommuner/OSLO", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await factory.Kodeverk.DidNotReceive().GetKommuneAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
