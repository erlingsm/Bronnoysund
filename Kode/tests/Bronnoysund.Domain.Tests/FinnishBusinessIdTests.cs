// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Domain.Tests;

public class FinnishBusinessIdTests
{
    // Real Finnish companies verified live against the PRH YTJ API on 2026-05-28.
    [Theory]
    [InlineData("0112038-9")] // Nokia Oyj
    [InlineData("2646674-9")] // Wolt Oy
    [InlineData("2336509-6")] // Supercell Oy
    public void TryCreate_ValidBusinessId_ReturnsTrue(string raw)
    {
        var ok = FinnishBusinessId.TryCreate(raw, out var value, out var error);

        ok.Should().BeTrue();
        value!.Value.Should().Be(raw);
        value.CountryCode.Should().Be("FI");
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("01120389", "0112038-9")]   // dashless input is normalised with the dash
    [InlineData(" 0112038-9 ", "0112038-9")] // padding is trimmed
    [InlineData("0112038 9", "0112038-9")]   // space instead of dash
    public void TryCreate_NormalisesToDashedForm(string input, string expected)
    {
        FinnishBusinessId.TryCreate(input, out var value, out _).Should().BeTrue();
        value!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_EmptyInput_ReturnsFalse(string? raw)
    {
        FinnishBusinessId.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("empty");
    }

    [Theory]
    [InlineData("123456")]      // 6 digits — too short
    [InlineData("123456789")]   // 9 digits — too long
    public void TryCreate_WrongLength_ReturnsFalse(string raw)
    {
        FinnishBusinessId.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("8 digits");
    }

    [Theory]
    [InlineData("0112038-0")] // wrong check digit (correct is 9)
    [InlineData("2646674-0")] // wrong check digit (correct is 9)
    public void TryCreate_WrongMod11_ReturnsFalse(string raw)
    {
        FinnishBusinessId.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("MOD11");
    }

    [Fact]
    public void Create_InvalidInput_Throws()
    {
        var act = () => FinnishBusinessId.Create("12345");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DigitsOnly_StripsTheDash()
    {
        FinnishBusinessId.Create("0112038-9").DigitsOnly.Should().Be("01120389");
    }

    [Fact]
    public void Equality_BasedOnNormalisedValue()
    {
        var dashed = FinnishBusinessId.Create("0112038-9");
        var plain = FinnishBusinessId.Create("01120389");

        dashed.Should().Be(plain);
        dashed.GetHashCode().Should().Be(plain.GetHashCode());
    }

    [Fact]
    public void IsACompanyIdentifier()
    {
        var fi = FinnishBusinessId.Create("0112038-9");

        fi.Should().BeAssignableTo<CompanyIdentifier>();
        ((CompanyIdentifier)fi).CountryCode.Should().Be("FI");
    }
}
