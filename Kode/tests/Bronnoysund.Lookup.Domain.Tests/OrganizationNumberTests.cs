// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Lookup.Domain.Tests;

public class OrganizationNumberTests
{
    [Theory]
    [InlineData("919300388")] // Equinor (gyldig, starter på 9)
    [InlineData("933722821")] // Røa Systemutvikling AS (gyldig, starter på 9)
    [InlineData("974760843")] // Statens vegvesen (gyldig, starter på 9)
    public void TryCreate_GyldigOrgNr_ReturnererTrue(string raw)
    {
        var ok = OrganizationNumber.TryCreate(raw, out var value, out var error);

        ok.Should().BeTrue();
        value.Value.Should().Be(raw);
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("919 300 388", "919300388")] // mellomrom fjernes
    [InlineData("919-300-388", "919300388")] // bindestrek fjernes
    [InlineData(" 919300388 ", "919300388")] // padding fjernes
    public void TryCreate_NormaliserSeparatorer(string input, string expected)
    {
        OrganizationNumber.TryCreate(input, out var value, out _).Should().BeTrue();
        value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryCreate_TomtInput_ReturnererFalse(string? raw)
    {
        OrganizationNumber.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("tom");
    }

    [Theory]
    [InlineData("12345")]      // for kort
    [InlineData("9193003881")] // for langt
    [InlineData("91930038")]   // 8 siffer
    public void TryCreate_FeilLengde_ReturnererFalse(string raw)
    {
        OrganizationNumber.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("9 siffer");
    }

    [Theory]
    [InlineData("12345678a")]
    [InlineData("abcdefghi")]
    public void TryCreate_IkkeBareTall_ReturnererFalse(string raw)
    {
        OrganizationNumber.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    [Theory]
    [InlineData("123456785")] // starter på 1
    [InlineData("712345678")] // starter på 7
    public void TryCreate_StarterIkkePå8Eller9_ReturnererFalse(string raw)
    {
        OrganizationNumber.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("8 eller 9");
    }

    [Theory]
    [InlineData("919300389")] // feil kontrollsiffer
    [InlineData("919300387")] // feil kontrollsiffer
    public void TryCreate_FeilMod11_ReturnererFalse(string raw)
    {
        OrganizationNumber.TryCreate(raw, out _, out var error).Should().BeFalse();
        error.Should().Contain("MOD11");
    }

    [Fact]
    public void Create_UgyldigInput_Kaster()
    {
        var act = () => OrganizationNumber.Create("12345");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Likhet_BasertPåNormalisertVerdi()
    {
        var a = OrganizationNumber.Create("919300388");
        var b = OrganizationNumber.Create("919 300 388");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnererNormalisertVerdi()
    {
        var orgnr = OrganizationNumber.Create("919 300 388");
        orgnr.ToString().Should().Be("919300388");
    }
}
