// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.International;
using FluentAssertions;

namespace Bronnoysund.Application.Tests;

public class CountryRoutingTests
{
    [Theory]
    [InlineData(null, "SE", "")]
    [InlineData("", "SE", "")]
    [InlineData("   ", "SE", "")]
    [InlineData("5560360793", null, "5560360793")]
    [InlineData("5560360793", "", "5560360793")]
    [InlineData("5560360793", "SE", "SE5560360793")]
    [InlineData("5560360793", "se", "SE5560360793")] // hint is case-insensitive
    [InlineData(" 5560360793 ", "SE", "SE5560360793")] // input is trimmed
    [InlineData("SE5560360793", "SE", "SE5560360793")] // already prefixed
    [InlineData("se5560360793", "SE", "se5560360793")] // existing prefix preserved verbatim
    [InlineData("919300388", "NO", "NO919300388")]
    [InlineData("12417834", "EE", "EE12417834")]
    public void ApplyPrefix_prepends_when_country_is_in_prefix_set(string? input, string? hint, string expected)
    {
        CountryRouting.ApplyPrefix(input, hint).Should().Be(expected);
    }

    [Theory]
    [InlineData("12345678", "IE")]
    [InlineData("10000000", "SI")]
    [InlineData("12345678901", "HR")]
    [InlineData("A12345674", "ES")]
    [InlineData("12345678", "RS")]
    public void ApplyPrefix_leaves_input_alone_for_non_prefix_countries(string input, string hint)
    {
        CountryRouting.ApplyPrefix(input, hint).Should().Be(input);
    }

    [Theory]
    [InlineData("12345678", true)]
    [InlineData("00000000", true)]
    [InlineData(" 12345678 ", true)] // trimmed
    [InlineData("1234567", false)] // 7 digits
    [InlineData("123456789", false)] // 9 digits
    [InlineData("1234567A", false)] // non-digit
    [InlineData("EE12417834", false)] // prefixed
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void LooksLikeEstonianAmbiguous_matches_only_bare_eight_digit_strings(string? input, bool expected)
    {
        CountryRouting.LooksLikeEstonianAmbiguous(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "text")]
    [InlineData("", "text")]
    [InlineData("ES", "text")]
    [InlineData("es", "text")] // case-insensitive
    [InlineData("GR", "text")]
    [InlineData("NO", "numeric")]
    [InlineData("FI", "numeric")]
    [InlineData("EE", "numeric")]
    [InlineData("PL", "numeric")]
    [InlineData("SE", "numeric")]
    [InlineData("DK", "numeric")]
    [InlineData("SI", "numeric")]
    [InlineData("LT", "numeric")]
    [InlineData("HR", "numeric")]
    [InlineData("LV", "numeric")]
    [InlineData("IT", "numeric")]
    [InlineData("RS", "numeric")]
    [InlineData("IE", "numeric")]
    public void InputModeFor_returns_text_for_ES_GR_and_numeric_for_the_rest(string? code, string expected)
    {
        CountryRouting.InputModeFor(code).Should().Be(expected);
    }
}
