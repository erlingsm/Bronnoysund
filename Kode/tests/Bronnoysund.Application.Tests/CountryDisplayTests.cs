// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.International;
using FluentAssertions;

namespace Bronnoysund.Application.Tests;

public class CountryDisplayTests
{
    [Theory]
    [InlineData("NO", "\U0001F1F3\U0001F1F4")] // 🇳🇴
    [InlineData("FI", "\U0001F1EB\U0001F1EE")] // 🇫🇮
    [InlineData("EE", "\U0001F1EA\U0001F1EA")] // 🇪🇪
    [InlineData("DK", "\U0001F1E9\U0001F1F0")] // 🇩🇰
    [InlineData("no", "\U0001F1F3\U0001F1F4")] // accepts lower-case
    public void Flag_returns_regional_indicator_pair_for_valid_iso(string iso, string expected)
    {
        CountryDisplay.Flag(iso).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N")]      // too short
    [InlineData("NOR")]    // too long
    [InlineData("N1")]     // non-letter
    [InlineData("123")]    // digits
    public void Flag_returns_white_flag_for_invalid_input(string? iso)
    {
        CountryDisplay.Flag(iso).Should().Be("\U0001F3F3"); // 🏳
    }
}
