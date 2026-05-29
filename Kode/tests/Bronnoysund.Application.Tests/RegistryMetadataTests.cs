// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.International;
using FluentAssertions;

namespace Bronnoysund.Application.Tests;

public class RegistryMetadataTests
{
    [Theory]
    [InlineData("NO")]
    [InlineData("FI")]
    [InlineData("EE")]
    [InlineData("IE")]
    [InlineData("PL")]
    [InlineData("SE")]
    [InlineData("DK")]
    [InlineData("SI")]
    [InlineData("LT")]
    [InlineData("HR")]
    [InlineData("GR")]
    [InlineData("LV")]
    [InlineData("ES")]
    [InlineData("IT")]
    [InlineData("RS")]
    public void For_returns_metadata_for_every_supported_country(string iso)
    {
        var info = RegistryMetadata.For(iso);
        info.Should().NotBeNull();
        info!.CountryCode.Should().Be(iso);
        info.RegistryName.Should().NotBeNullOrWhiteSpace();
        info.LicenceShort.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void For_accepts_lowercase()
    {
        RegistryMetadata.For("no").Should().NotBeNull();
        RegistryMetadata.For("Se").Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("XX")]
    [InlineData("USA")]
    public void For_returns_null_for_unknown_or_empty(string? iso)
    {
        RegistryMetadata.For(iso).Should().BeNull();
    }

    private static readonly string[] ExpectedCountries =
        ["NO", "FI", "EE", "IE", "PL", "SE", "DK", "SI", "LT", "HR", "GR", "LV", "ES", "IT", "RS"];

    [Fact]
    public void SupportedCountries_matches_the_table()
    {
        RegistryMetadata.SupportedCountries.Should().BeEquivalentTo(ExpectedCountries);
    }
}
