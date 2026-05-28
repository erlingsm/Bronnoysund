// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Domain;

namespace Bronnoysund.Infrastructure.Detection;

/// <summary>
/// Best-effort identifier-to-country detector. Tries each registered country pattern in
/// priority order and returns the first match. Where two countries can match the same
/// raw input (8-digit DK/EE, 11-digit HR/IT/LV, 9-digit NO/GR/LT/PL/RS, …) the detector
/// commits to the most-likely candidate for our user base; the UI provides an explicit
/// country override (Plan 26 B2) for the rest.
/// </summary>
/// <remarks>
/// Detection priority hierarchy:
/// 1. Strongest checksum + most-specific prefix wins (Norwegian MOD-11 + leading 8/9).
/// 2. Mandatory separators win next (Finnish dash, Spanish entity-letter, EU VAT prefix).
/// 3. Unique-length identifiers (Greek 12-digit GEMI, Polish 14-digit REGON).
/// 4. For length-collision groups, stricter checksum (ISO 7064) beats weaker (Luhn)
///    beats format-only. Ties broken by user-base likelihood (Nordic preferred).
/// 5. Last-resort fallbacks (Irish 1-7 digits without prefix) come last.
/// Known unavoidable ambiguities (no algorithmic discriminator exists):
///   * DK CVR vs EE registrikood for 8-digit strings starting with 1/7/8/9 — default DK
///   * RS matični broj is 8-digit format-only and would catch everything 8-digit if
///     placed too early; ordered behind both DK and EE.
/// </remarks>
internal sealed class CountryDetector : ICountryDetector
{
    public CompanyIdentifier? Detect(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return null;
        }

        // -- Highly-specific prefixes win without a length check ----------------------

        // Norwegian — 9 digits + MOD-11 + leading 8/9 is unambiguous.
        if (OrganizationNumber.TryCreate(rawInput, out var organizationNumber, out _))
        {
            return organizationNumber;
        }

        // Spanish NIF — starts with an entity-type letter (A-W minus a few). The letter
        // prefix eliminates any collision with pure-numeric IDs.
        if (SpanishNif.TryCreate(rawInput, out var spanish, out _))
        {
            return spanish;
        }

        // Explicit EU VAT-style prefixes commit immediately. Without these short-circuits,
        // "LV40003032949" (Latvenergo) would pass Italian Luhn and be misdetected as IT.
        var trimmed = rawInput.TrimStart();
        if (StartsWithIgnoreCase(trimmed, "LV") &&
            LatvianRegistrationNumber.TryCreate(rawInput, out var lvPrefixed, out _))
        {
            return lvPrefixed;
        }
        if ((StartsWithIgnoreCase(trimmed, "EL") || StartsWithIgnoreCase(trimmed, "GR")) &&
            GreekVatNumber.TryCreate(rawInput, out var grPrefixed, out _))
        {
            return grPrefixed;
        }
        if (StartsWithIgnoreCase(trimmed, "IT") &&
            ItalianFiscalCode.TryCreate(rawInput, out var itPrefixed, out _))
        {
            return itPrefixed;
        }

        // Finnish Y-tunnus requires the explicit dash to commit (the dashless form is
        // ambiguous with Estonian/Danish 8-digit codes).
        if (rawInput.Contains('-') &&
            FinnishBusinessId.TryCreate(rawInput, out var finnishBusinessId, out _))
        {
            return finnishBusinessId;
        }

        // -- Numeric-only IDs grouped by digit-count -----------------------------------

        var digits = new string(rawInput.Where(char.IsDigit).ToArray());

        // 14-digit: Polish REGON-14 is the only such pattern we know.
        if (digits.Length == 14 &&
            PolishRegon.TryCreate(rawInput, out var regon14, out _))
        {
            return regon14;
        }

        // 12-digit: Greek GEMI is unique.
        if (digits.Length == 12 &&
            GreekGemiNumber.TryCreate(rawInput, out var gemi, out _))
        {
            return gemi;
        }

        // 11-digit: try HR (ISO 7064 MOD 11,10 — strongest), then IT (Luhn), then LV
        // (format-only, leading 4 or 5). HR and IT have non-overlapping valid sets in
        // practice because the algorithms differ; LV's leading-4/5 guard avoids most
        // collisions with either.
        if (digits.Length == 11)
        {
            if (CroatianOib.TryCreate(rawInput, out var croatian, out _)) return croatian;
            if (ItalianFiscalCode.TryCreate(rawInput, out var italian, out _)) return italian;
            if (LatvianRegistrationNumber.TryCreate(rawInput, out var latvian, out _)) return latvian;
        }

        // 10-digit: KRS-with-leading-0000 is the only structural signal we have. PL NIP
        // (MOD-11) wins next, then SE (Luhn), then SI (format-only). The unconditional
        // PL KRS catch-all that earlier swallowed every 10-digit string has been removed
        // (code-review-2026-05-29 critical #1).
        if (digits.Length == 10)
        {
            if (digits.StartsWith("0000", StringComparison.Ordinal) &&
                PolishKrsNumber.TryCreate(rawInput, out var krs, out _))
            {
                return krs;
            }
            if (PolishNip.TryCreate(rawInput, out var nip, out _)) return nip;
            if (SwedishOrganizationNumber.TryCreate(rawInput, out var swedish, out _)) return swedish;
            if (SlovenianMaticnaStevilka.TryCreate(rawInput, out var slovenian, out _)) return slovenian;
        }

        // 9-digit: GR AFM (powers-of-2 MOD-11) → LT (two-step MOD-11) → PL REGON-9
        // (MOD-11 with [8,9,2,3,4,5,6,7] weights) → RS PIB (ISO 7064 MOD 11,10). The
        // four checksums rarely collide; for the cases where they do, we prefer the
        // earliest in the list. NO orgnr was already handled above.
        if (digits.Length == 9)
        {
            if (GreekVatNumber.TryCreate(rawInput, out var afm, out _)) return afm;
            if (LithuanianCompanyCode.TryCreate(rawInput, out var lithuanian, out _)) return lithuanian;
            if (PolishRegon.TryCreate(rawInput, out var regon9, out _)) return regon9;
            if (SerbianPib.TryCreate(rawInput, out var pib, out _)) return pib;
        }

        // 8-digit: DK CVR (format-only, non-zero leading) wins by default because the
        // Nordic market is the larger user base. EE registrikood (format ^[1789]\d{7}$)
        // is unreachable from this detector path — it requires the UI's explicit country
        // override or a future "EE" prefix on the value object. RS matični broj only
        // catches plain 8-digit input — a dashed input that fell through Finnish above
        // (Finnish-shape attempted but checksum-failed) should not silently become RS.
        if (digits.Length == 8)
        {
            if (DanishCvrNumber.TryCreate(rawInput, out var danish, out _)) return danish;
            if (EstonianRegistryCode.TryCreate(rawInput, out var estonian, out _)) return estonian;
            if (!rawInput.Contains('-') &&
                SerbianMaticniBroj.TryCreate(rawInput, out var mb, out _))
            {
                return mb;
            }
        }

        // Irish CRO (1-7 digits without prefix). Last resort — a bare "5" matches IE
        // but also matches many partial inputs. We accept the false-positive risk
        // because the alternative (no detection) is worse, and Plan 26's UI flag
        // indicator surfaces the (sometimes wrong) detection back to the user.
        if (IrishCroNumber.TryCreate(rawInput, out var irish, out _))
        {
            return irish;
        }

        return null;
    }

    private static bool StartsWithIgnoreCase(string s, string prefix) =>
        s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
