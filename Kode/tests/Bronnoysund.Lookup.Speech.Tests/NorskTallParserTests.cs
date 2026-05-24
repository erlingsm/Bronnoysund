// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Speech.Parsing;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Lookup.Speech.Tests;

public class NorskTallParserTests
{
    [Theory]
    [InlineData("ni en ni tre null null tre åtte åtte", "919300388")]
    [InlineData("ni ett ni tre null null tre åtte åtte", "919300388")]
    [InlineData("ni en ni tre null null tre otte otte", "919300388")] // diakritikk-fri "otte"
    [InlineData("nine one nine three zero zero three eight eight", "919300388")]
    [InlineData("919300388", "919300388")] // ren sifferstreng
    [InlineData("919 300 388", "919300388")] // mellomrom-separert
    [InlineData("ni-en-ni-tre-null-null-tre-åtte-åtte", "919300388")] // bindestrek
    public void Norske_Sifferord_Mappes_Til_Siffer(string input, string expected)
    {
        var ok = NorskTallParser.TryParseDigits(input, out var digits);
        ok.Should().BeTrue();
        digits.Should().Be(expected);
    }

    [Theory]
    [InlineData("ni en ni tre", "9193")]
    [InlineData("null", "0")]
    [InlineData("syv", "7")]
    [InlineData("sju", "7")]
    public void Delvise_Tall_Mappes_Korrekt(string input, string expected)
    {
        NorskTallParser.TryParseDigits(input, out var digits).Should().BeTrue();
        digits.Should().Be(expected);
    }

    [Theory]
    [InlineData("hei verden")]
    [InlineData("ni en hundre")] // "hundre" støttes ikke i siffer-mode
    [InlineData("artisan consulting as")]
    public void Ukjente_Ord_Returnerer_False(string input)
    {
        NorskTallParser.TryParseDigits(input, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Tom_Input_Returnerer_False(string input)
    {
        NorskTallParser.TryParseDigits(input, out _).Should().BeFalse();
    }
}
