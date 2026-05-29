// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Collections.Frozen;

namespace Bronnoysund.Application.International;

/// <summary>
/// Static catalogue of which national registry each ICompanyProvider talks to, plus the
/// licence the underlying data set is published under. Used by the Lookup result footer
/// (Plan 26, Trinn B3) to give every lookup a clear "Source: {registry} • Licence: {licence}"
/// stamp. Kept hard-coded — the table changes on the order of years, and threading it through
/// Azure App Config adds operational risk without payoff. New countries get an entry here at
/// the same time a new <c>ICompanyProvider</c> is registered.
/// </summary>
public static class RegistryMetadata
{
    public sealed record RegistryInfo(string CountryCode, string RegistryName, string LicenceShort);

    private static readonly FrozenDictionary<string, RegistryInfo> Table =
        new Dictionary<string, RegistryInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["NO"] = new("NO", "Brønnøysundregistrene",       "NLOD 2.0"),
            ["FI"] = new("FI", "PRH / YTJ",                   "CC-BY 4.0"),
            ["EE"] = new("EE", "Äriregister",                 "Estonian State Portal terms"),
            ["IE"] = new("IE", "CRO (data.gov.ie)",           "CC-BY 4.0"),
            ["PL"] = new("PL", "KRS + CEIDG",                 "CC-BY 4.0"),
            ["SE"] = new("SE", "Bolagsverket",                "CC-BY 4.0"),
            ["DK"] = new("DK", "CVR (Virk.dk)",               "CC0 1.0"),
            ["SI"] = new("SI", "AJPES PRS",                   "AJPES terms"),
            ["LT"] = new("LT", "Registrų Centras",            "CC-BY 4.0"),
            ["HR"] = new("HR", "Sudski Registar",             "Croatian MoJ terms"),
            ["GR"] = new("GR", "GEMI",                        "Greek MoD terms"),
            ["LV"] = new("LV", "Lursoft / UR",                "Lursoft licence"),
            ["ES"] = new("ES", "OpenCorporates (RMC)",        "ODbL 1.0"),
            ["IT"] = new("IT", "OpenCorporates (InfoCamere)", "ODbL 1.0"),
            ["RS"] = new("RS", "OpenCorporates (APR)",        "ODbL 1.0"),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the registry metadata for an ISO 3166-1 alpha-2 country code, or <c>null</c>
    /// if the code isn't in the registered set. Callers should fall back gracefully — Plan 26
    /// suggests skipping the kildemerking footer rather than rendering "Unknown".
    /// </summary>
    public static RegistryInfo? For(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode) ? null
            : Table.TryGetValue(countryCode, out var info) ? info : null;

    /// <summary>All registered country codes, useful for the country picker (Trinn B2).</summary>
    public static IReadOnlyCollection<string> SupportedCountries => Table.Keys;
}
