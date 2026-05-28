// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Domain.Tests;

public class CroatianOibTests
{
    [Theory]
    [InlineData("27759560625")]  // INA d.d.
    [InlineData("71149912416")]  // Atlantic Grupa
    [InlineData("46348534278")]  // Wikipedia reference
    public void TryCreate_Valid(string raw)
    {
        CroatianOib.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("HR");
    }

    [Theory]
    [InlineData("12345678901")]  // checksum fail
    [InlineData("1234")]
    public void TryCreate_Invalid(string raw)
    {
        CroatianOib.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class GreekIdentifierTests
{
    [Theory]
    [InlineData("094019245")]  // OTE
    [InlineData("EL094019245")] // with VAT prefix
    public void Afm_Valid(string raw)
    {
        GreekVatNumber.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("GR");
    }

    [Theory]
    [InlineData("094019244")]  // mutated check
    public void Afm_Invalid(string raw)
    {
        GreekVatNumber.TryCreate(raw, out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("154558160000")]  // Eurobank S.A.
    [InlineData("000223001000")]  // Eurobank Holdings (leading zeros)
    public void Gemi_Valid(string raw)
    {
        GreekGemiNumber.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.Value.Should().HaveLength(12);
    }
}

public class LatvianRegistrationNumberTests
{
    [Theory]
    [InlineData("40003245752")]   // Air Baltic
    [InlineData("LV40003032949")] // VAT-prefixed
    public void TryCreate_Valid(string raw)
    {
        LatvianRegistrationNumber.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("LV");
    }

    [Theory]
    [InlineData("10003245752")]   // starts with 1, not 4/5
    [InlineData("123")]
    public void TryCreate_Invalid(string raw)
    {
        LatvianRegistrationNumber.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class SpanishNifTests
{
    [Theory]
    [InlineData("A28015865")]  // Telefónica
    [InlineData("A48265169")]  // BBVA
    public void TryCreate_Valid(string raw)
    {
        SpanishNif.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("ES");
    }

    [Theory]
    [InlineData("123456789")]  // missing letter prefix
    public void TryCreate_Invalid(string raw)
    {
        SpanishNif.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class ItalianFiscalCodeTests
{
    [Theory]
    [InlineData("00159560366")]  // Ferrari S.p.A.
    [InlineData("00905811006")]  // Eni S.p.A.
    [InlineData("IT00905811006")] // with VAT prefix
    public void TryCreate_Valid(string raw)
    {
        ItalianFiscalCode.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("IT");
    }

    [Theory]
    [InlineData("00159560367")]  // mutated check
    public void TryCreate_Invalid(string raw)
    {
        ItalianFiscalCode.TryCreate(raw, out _, out _).Should().BeFalse();
    }
}

public class SerbianIdentifierTests
{
    [Theory]
    [InlineData("20084693")]  // NIS a.d.
    [InlineData("07069461")]  // Telekom Srbija
    public void MaticniBroj_Valid(string raw)
    {
        SerbianMaticniBroj.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("RS");
    }

    [Theory]
    [InlineData("104052135")]  // NIS PIB
    [InlineData("100002887")]  // Telekom Srbija PIB
    public void Pib_Valid(string raw)
    {
        SerbianPib.TryCreate(raw, out var v, out _).Should().BeTrue();
        v!.CountryCode.Should().Be("RS");
    }
}
