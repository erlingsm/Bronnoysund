// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Speech.Parsing;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Speech.Tests;

public class NorskTallParserTests
{
    [Theory]
    [InlineData("ni en ni tre null null tre åtte åtte", "919300388")]
    [InlineData("ni ett ni tre null null tre åtte åtte", "919300388")]
    [InlineData("ni en ni tre null null tre otte otte", "919300388")] // diacritic-free "otte"
    [InlineData("nine one nine three zero zero three eight eight", "919300388")]
    [InlineData("919300388", "919300388")] // plain digit string
    [InlineData("919 300 388", "919300388")] // space-separated
    [InlineData("ni-en-ni-tre-null-null-tre-åtte-åtte", "919300388")] // hyphen
    public void NorwegianDigitWords_MapToDigits(string input, string expected)
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
    public void PartialNumbers_MapCorrectly(string input, string expected)
    {
        NorskTallParser.TryParseDigits(input, out var digits).Should().BeTrue();
        digits.Should().Be(expected);
    }

    [Theory]
    [InlineData("hei verden")]
    [InlineData("ni en hundre")] // "hundre" is not supported in digit mode
    [InlineData("artisan consulting as")]
    public void UnknownWords_ReturnFalse(string input)
    {
        NorskTallParser.TryParseDigits(input, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInput_ReturnsFalse(string input)
    {
        NorskTallParser.TryParseDigits(input, out _).Should().BeFalse();
    }
}
