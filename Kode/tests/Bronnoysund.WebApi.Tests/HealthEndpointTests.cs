// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Net.Http.Json;
using Bronnoysund.WebApi.Tests.Infrastructure;
using FluentAssertions;

namespace Bronnoysund.WebApi.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOkWithServiceAndStatus()
    {
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.status.Should().Be("ok");
        body.service.Should().Be("Bronnoysund.WebApi");
    }

    [Fact]
    public async Task GetHealthProviders_ListsSupportedCountriesFromRegistry()
    {
        using var factory = new WebApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/providers", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProvidersResponse>();
        body.Should().NotBeNull();
        // The single replaced ICompanyProvider fake reports CountryCode = "NO" via
        // WebApiFactory's default seed. Real production wiring registers both NO and FI.
        body!.SupportedCountries.Should().Contain("NO");
    }

    private sealed record HealthResponse(string status, string service);

    private sealed record ProvidersResponse(string[] SupportedCountries);
}
