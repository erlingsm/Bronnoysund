// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.International;

/// <summary>
/// Presentation helpers for ISO 3166-1 alpha-2 country codes. The UI uses these to render
/// flag-indicator badges next to a detected identifier (Plan 26, Trinn B1) and inside the
/// country picker (Trinn B2). Country names are localized via <c>Country.{code}</c> resx
/// keys; this class only owns the script-independent flag-emoji mapping.
/// </summary>
public static class CountryDisplay
{
    /// <summary>
    /// Returns the Unicode flag emoji for a two-letter country code by composing two Regional
    /// Indicator Symbols (U+1F1E6..U+1F1FF). On platforms whose font has the flag glyphs this
    /// renders as a single colour flag (NO -> 🇳🇴, FI -> 🇫🇮, …). Returns the white-flag
    /// fallback (🏳) for input that is not exactly two ASCII letters.
    /// </summary>
    /// <remarks>
    /// On Windows &lt; 11 and Linux without Twemoji the regional-indicator pair renders as
    /// two blank squares (one per letter). Every UI call-site pairs the flag with either a
    /// localized country name (picker list, detection badges, B3 footer) or the ISO code
    /// itself (picker activator). The accompanying text carries the semantic load, so the
    /// "no flag-glyph" state degrades to plain text rather than to "unknown country" —
    /// CR N5 confirmed this pattern is graceful on the bare-flag-fallback platforms.
    /// </remarks>
    public static string Flag(string? isoAlpha2)
    {
        if (string.IsNullOrEmpty(isoAlpha2) || isoAlpha2.Length != 2) return "\U0001F3F3";
        var c0 = char.ToUpperInvariant(isoAlpha2[0]);
        var c1 = char.ToUpperInvariant(isoAlpha2[1]);
        if (c0 < 'A' || c0 > 'Z' || c1 < 'A' || c1 > 'Z') return "\U0001F3F3";
        return char.ConvertFromUtf32(0x1F1E6 + (c0 - 'A')) + char.ConvertFromUtf32(0x1F1E6 + (c1 - 'A'));
    }
}
