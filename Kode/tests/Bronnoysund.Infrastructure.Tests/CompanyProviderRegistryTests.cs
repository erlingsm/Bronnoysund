// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class CompanyProviderRegistryTests
{
    [Fact]
    public void GetForCountry_KnownCode_ReturnsProvider()
    {
        var no = new FakeProvider("NO");
        var fi = new FakeProvider("FI");
        var registry = new CompanyProviderRegistry([no, fi]);

        registry.GetForCountry("NO").Should().BeSameAs(no);
        registry.GetForCountry("FI").Should().BeSameAs(fi);
    }

    [Fact]
    public void GetForCountry_IsCaseInsensitive()
    {
        var no = new FakeProvider("NO");
        var registry = new CompanyProviderRegistry([no]);

        registry.GetForCountry("no").Should().BeSameAs(no);
        registry.GetForCountry("No").Should().BeSameAs(no);
    }

    [Fact]
    public void GetForCountry_UnknownCode_ReturnsNull()
    {
        var registry = new CompanyProviderRegistry([new FakeProvider("NO")]);

        registry.GetForCountry("XX").Should().BeNull();
    }

    [Fact]
    public void SupportedCountries_ExposesAllRegistered()
    {
        var registry = new CompanyProviderRegistry(
            [new FakeProvider("NO"), new FakeProvider("FI"), new FakeProvider("PL")]);

        registry.SupportedCountries.Should().BeEquivalentTo("NO", "FI", "PL");
    }

    [Fact]
    public void Constructor_DuplicateCountryCode_Throws()
    {
        // A misconfigured composition root is worse than a crash on startup —
        // silently dropping one of the providers would surface as mysterious "country
        // not supported" errors at runtime.
        var act = () => new CompanyProviderRegistry(
            [new FakeProvider("NO"), new FakeProvider("NO")]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*claim country code 'NO'*");
    }

    private sealed class FakeProvider(string country) : ICompanyProvider
    {
        public string CountryCode => country;

        public Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct) =>
            Task.FromResult<CompanyLookupResult>(new CompanyLookupResult.NotFound(id.Value));
    }
}
