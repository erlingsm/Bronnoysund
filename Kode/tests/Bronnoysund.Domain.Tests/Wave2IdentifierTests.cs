// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Domain.Tests;

public class SwedishOrganizationNumberTests
{
    [Theory]
    [InlineData("5560360793")]   // Volvo AB
    [InlineData("5567370431")]   // Klarna AB
    [InlineData("556036-0793")]  // dash accepted, normalised away
    public void TryCreate_Valid(string raw)
    {
        SwedishOrganizationNumber.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("SE");
        v.Value.Should().HaveLength(10);
    }

    [Theory]
    [InlineData("5560360794")] // mutated check digit
    [InlineData("123")]
    public void TryCreate_Invalid(string raw)
    {
        SwedishOrganizationNumber.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class DanishCvrNumberTests
{
    [Theory]
    [InlineData("28856713")] // Maersk
    [InlineData("33063295")] // Carlsberg
    public void TryCreate_Valid(string raw)
    {
        DanishCvrNumber.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("DK");
    }

    [Theory]
    [InlineData("123")]       // too short
    [InlineData("123456789")] // too long
    public void TryCreate_Invalid(string raw)
    {
        DanishCvrNumber.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class SlovenianMaticnaStevilkaTests
{
    [Theory]
    [InlineData("5043611000")] // KRKA
    [InlineData("5025796000")] // Petrol
    [InlineData("5860571000")] // Telekom Slovenije
    public void TryCreate_Valid(string raw)
    {
        SlovenianMaticnaStevilka.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("SI");
        v.NormalizedRoot.Should().HaveLength(7);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc12345xx")]
    public void TryCreate_Invalid(string raw)
    {
        SlovenianMaticnaStevilka.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class LithuanianCompanyCodeTests
{
    [Theory]
    [InlineData("120545849")] // Vilniaus Vandenys
    [InlineData("121215434")] // Telia Lietuva
    public void TryCreate_Valid(string raw)
    {
        LithuanianCompanyCode.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("LT");
    }

    [Theory]
    [InlineData("120545840")] // mutated checksum
    [InlineData("12345")]
    public void TryCreate_Invalid(string raw)
    {
        LithuanianCompanyCode.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}
