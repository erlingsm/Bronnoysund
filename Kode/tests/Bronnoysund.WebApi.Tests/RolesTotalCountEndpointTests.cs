// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bronnoysund.Application.Results;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// H1: End-to-end coverage for <c>/statistics/roles-total-count</c>. Verifies that the
/// Found arm wraps the scalar in a <c>{ totalCount: <long> }</c> object (H12 design choice)
/// and that Unavailable maps to 503 ProblemDetails.
/// </summary>
public class RolesTotalCountEndpointTests
{
    [Fact]
    public async Task GetRolesTotalCount_Found_ReturnsOkWithWrappedScalarShape()
    {
        using var factory = new WebApiFactory();
        factory.Statistics.GetRolesTotalCountAsync(Arg.Any<CancellationToken>())
            .Returns(new RolesTotalCountResult.Found(4711000L));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/statistics/roles-total-count", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Object, "the scalar must be wrapped in a JSON object per H12");
        root.GetProperty("totalCount").GetInt64().Should().Be(4711000L);
    }

    [Fact]
    public async Task GetRolesTotalCount_Unavailable_Returns503ProblemDetails()
    {
        using var factory = new WebApiFactory();
        factory.Statistics.GetRolesTotalCountAsync(Arg.Any<CancellationToken>())
            .Returns(new RolesTotalCountResult.Unavailable("Brreg circuit open."));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/statistics/roles-total-count", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        problem!.detail.Should().Contain("circuit open");
    }
}
