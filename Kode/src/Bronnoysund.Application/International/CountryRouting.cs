// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.International;

/// <summary>
/// Routing helpers for the Lookup page (Plan 26). Kept in <c>Application.International</c>
/// so the ViewModel and the Razor page share one implementation, and so the logic is
/// unit-testable without spinning up Blazor.
/// </summary>
public static class CountryRouting
{
    /// <summary>
    /// ISO 3166-1 alpha-2 codes whose value objects accept a VAT-style prefix in
    /// <c>ICountryDetector.Detect</c>. When the user picks one of these from the country
    /// picker (Plan 26 B2) and the typed input lacks the prefix, <see cref="ApplyPrefix"/>
    /// prepends it so the existing detector still routes correctly. Countries outside this
    /// set (IE, SI, HR, ES, RS) rely on distinctive format heuristics — manual selection
    /// is currently visual and doesn't change routing for those.
    /// </summary>
    public static IReadOnlySet<string> PrefixCountries { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "NO", "SE", "FI", "DK", "EE", "LV", "PL", "GR", "IT",
        };

    /// <summary>
    /// Returns <paramref name="input"/> with the user-selected country's VAT prefix
    /// prepended when needed. Returns the trimmed input unchanged when no override is
    /// active, when the override isn't prefix-supported, or when the prefix is already
    /// present.
    /// </summary>
    public static string ApplyPrefix(string? input, string? selectedCountryHint)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || string.IsNullOrEmpty(selectedCountryHint))
        {
            return trimmed;
        }
        if (!PrefixCountries.Contains(selectedCountryHint))
        {
            return trimmed;
        }
        if (trimmed.StartsWith(selectedCountryHint, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }
        return selectedCountryHint.ToUpperInvariant() + trimmed;
    }

    /// <summary>
    /// True when <paramref name="input"/> is exactly eight digits. Drives the "Estonian
    /// number? Try EE prefix" hint (Plan 26 B1) — the Danish CVR and Estonian registry
    /// codes share the eight-digit shape, and the detector picks Denmark first.
    /// </summary>
    public static bool LooksLikeEstonianAmbiguous(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim();
        return trimmed.Length == 8 && trimmed.All(char.IsDigit);
    }

    /// <summary>
    /// Returns <c>"numeric"</c> or <c>"text"</c> for the HTML <c>inputmode</c> attribute
    /// (Plan 26 M2). Numeric covers the thirteen jurisdictions whose business IDs are
    /// pure digit strings; ES (NIF entity-letter prefix) and GR (EL/GR VAT prefix) plus
    /// the "no country chosen yet" empty state fall back to text.
    /// </summary>
    public static string InputModeFor(string? countryCode)
    {
        if (string.IsNullOrEmpty(countryCode)) return "text";
        return countryCode.ToUpperInvariant() switch
        {
            "ES" or "GR" => "text",
            _ => "numeric",
        };
    }
}
