// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.International;
using Bronnoysund.Application.Ports;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Pins the invariant that the country set registered via
/// <see cref="ServiceCollectionExtensions.AddBronnoysundInfrastructure"/> matches the set
/// in <see cref="RegistryMetadata"/>. If a new ICompanyProvider is added without a
/// corresponding metadata entry, the Lookup result footer would silently drop
/// "Source: … • Licence: …" for that country (Plan 26 B3).
/// </summary>
public class RegistryMetadataSyncTests
{
    [Fact]
    public void Every_registered_country_has_RegistryMetadata_entry()
    {
        var registry = BuildRegistry();
        foreach (var code in registry.SupportedCountries)
        {
            RegistryMetadata.For(code).Should().NotBeNull(
                because: $"country {code} is registered as an ICompanyProvider but " +
                         $"RegistryMetadata.For(\"{code}\") returned null. The B3 footer " +
                         $"will not render a Source/Licence line for that country.");
        }
    }

    [Fact]
    public void Every_RegistryMetadata_entry_has_a_registered_provider()
    {
        var registry = BuildRegistry();
        foreach (var code in RegistryMetadata.SupportedCountries)
        {
            registry.GetForCountry(code).Should().NotBeNull(
                because: $"RegistryMetadata advertises {code} but no ICompanyProvider " +
                         $"is registered for it. A lookup that hits that country would " +
                         $"return Unavailable(\"no provider registered\").");
        }
    }

    private static ICompanyProviderRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();
        services.AddBronnoysundInfrastructure(configuration);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ICompanyProviderRegistry>();
    }
}
