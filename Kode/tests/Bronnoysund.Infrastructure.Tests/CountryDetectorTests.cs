// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using Bronnoysund.Infrastructure.Detection;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class CountryDetectorTests
{
    private readonly CountryDetector _detector = new();

    [Theory]
    [InlineData("919300388")]     // Equinor
    [InlineData("919 300 388")]   // with separators — value object normalises
    [InlineData("933722821")]     // Røa Systemutvikling AS
    public void Detect_NorwegianOrganizationNumber_ReturnsOrganizationNumber(string raw)
    {
        var result = _detector.Detect(raw);

        result.Should().BeOfType<OrganizationNumber>();
        result!.CountryCode.Should().Be("NO");
    }

    [Theory]
    [InlineData("0112038-9")] // Nokia Oyj
    [InlineData("2646674-9")] // Wolt Oy
    [InlineData("2336509-6")] // Supercell Oy
    public void Detect_FinnishBusinessId_ReturnsFinnishBusinessId(string raw)
    {
        var result = _detector.Detect(raw);

        result.Should().BeOfType<FinnishBusinessId>();
        result!.CountryCode.Should().Be("FI");
    }

    [Fact]
    public void Detect_FinnishDigitsWithoutDash_ReturnsNull()
    {
        // 8 plain digits could be Estonian/Danish — we require the dash to commit to FI.
        _detector.Detect("01120389").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a number")]
    [InlineData("12345")]          // too short
    [InlineData("123456785")]      // starts with 1 — fails NO rule
    [InlineData("0112038-0")]      // looks Finnish but wrong checksum
    public void Detect_UnknownOrInvalidInput_ReturnsNull(string? raw)
    {
        _detector.Detect(raw).Should().BeNull();
    }
}
