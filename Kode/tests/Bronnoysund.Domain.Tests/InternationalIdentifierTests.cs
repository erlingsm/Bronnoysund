// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Domain.Tests;

public class EstonianRegistryCodeTests
{
    [Theory]
    [InlineData("12417834")]  // Bolt Technology OÜ
    [InlineData("11056239")]  // Microsoft Development Center Estonia OÜ
    [InlineData("80012345")]  // non-profit MTÜ shape
    [InlineData("90012345")]  // foundation SA shape
    [InlineData("70012345")]  // public agency shape
    public void TryCreate_ValidShape_ReturnsTrue(string raw)
    {
        EstonianRegistryCode.TryCreate(raw, out var value, out _).Should().BeTrue();
        value!.CountryCode.Should().Be("EE");
        value.Value.Should().Be(raw);
    }

    [Theory]
    [InlineData(null, "empty")]
    [InlineData("", "empty")]
    [InlineData("12345", "8 digits")]
    [InlineData("20012345", "8 digits")] // starts with 2 — not in {1,7,8,9}
    [InlineData("60012345", "8 digits")]
    public void TryCreate_Invalid_ReturnsFalse(string? raw, string errorFragment)
    {
        EstonianRegistryCode.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain(errorFragment);
    }

    [Fact]
    public void Kind_DerivedFromFirstDigit()
    {
        EstonianRegistryCode.Create("12417834").Kind.Should().Be(EntityKind.Company);
        EstonianRegistryCode.Create("70012345").Kind.Should().Be(EntityKind.PublicAgency);
        EstonianRegistryCode.Create("80012345").Kind.Should().Be(EntityKind.NonProfit);
        EstonianRegistryCode.Create("90012345").Kind.Should().Be(EntityKind.Foundation);
    }

    [Fact]
    public void IsACompanyIdentifier()
    {
        EstonianRegistryCode.Create("12417834").Should().BeAssignableTo<CompanyIdentifier>();
    }
}

public class IrishCroNumberTests
{
    [Theory]
    [InlineData("5")]        // eldest registered AS
    [InlineData("408059")]   // Microsoft Ireland Operations
    [InlineData("593876")]   // Stripe Payments Europe
    [InlineData("9999999")]  // upper bound
    public void TryCreate_Valid_ReturnsTrue(string raw)
    {
        IrishCroNumber.TryCreate(raw, out var value, out _).Should().BeTrue();
        value!.CountryCode.Should().Be("IE");
        value.Value.Should().Be(raw);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678")]  // 8 digits — too long
    [InlineData("0123")]      // leading zero on a multi-digit value
    public void TryCreate_Invalid_ReturnsFalse(string? raw)
    {
        IrishCroNumber.TryCreate(raw, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void IsACompanyIdentifier()
    {
        IrishCroNumber.Create("408059").Should().BeAssignableTo<CompanyIdentifier>();
    }
}

public class PolishIdentifiersTests
{
    [Theory]
    [InlineData("0000028860")]  // PKN Orlen
    [InlineData("0000059492")]  // CD Projekt
    [InlineData("0000031276")]  // PKO BP
    public void Krs_Valid(string raw)
    {
        PolishKrsNumber.TryCreate(raw, out var value, out _).Should().BeTrue();
        value!.CountryCode.Should().Be("PL");
    }

    [Theory]
    [InlineData("7740001454")]  // PKN Orlen NIP
    [InlineData("5260250274")]  // Allegro NIP (live-verified)
    public void Nip_Valid(string raw)
    {
        PolishNip.TryCreate(raw, out var value, out _).Should().BeTrue();
        value!.CountryCode.Should().Be("PL");
    }

    [Theory]
    [InlineData("7740001455")]  // mutated checksum
    [InlineData("7740001459")]  // mutated checksum
    public void Nip_InvalidChecksum_ReturnsFalse(string raw)
    {
        PolishNip.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("MOD-11");
    }

    [Theory]
    [InlineData("610188201")]   // PKN Orlen 9-digit REGON
    [InlineData("492707333")]   // CD Projekt 9-digit REGON
    public void Regon9_Valid(string raw)
    {
        PolishRegon.TryCreate(raw, out var value, out _).Should().BeTrue();
        value!.CountryCode.Should().Be("PL");
    }

    [Fact]
    public void Krs_LeadingZerosPreserved()
    {
        PolishKrsNumber.Create("0000028860").Value.Should().Be("0000028860");
    }
}
